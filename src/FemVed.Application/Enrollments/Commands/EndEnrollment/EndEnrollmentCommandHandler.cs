using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Enrollments.Commands.EndEnrollment;

/// <summary>
/// Handles <see cref="EndEnrollmentCommand"/>.
/// <list type="number">
///   <item>Verifies the enrollment exists and the caller is authorised to end it.</item>
///   <item>Guards against invalid state transitions (must be ACTIVE or PAUSED).</item>
///   <item>Sets Status = COMPLETED, CompletedAt = now, EndedBy + EndedByRole.</item>
///   <item>Appends a <see cref="ProgramSessionLog"/> entry.</item>
///   <item>Emails the enrolled user via the <c>session_ended</c> SendGrid template.</item>
/// </list>
/// </summary>
public sealed class EndEnrollmentCommandHandler : IRequestHandler<EndEnrollmentCommand>
{
    // Google Forms feedback links — sent to enrolled user / expert when a program ends.
    // Update here if the form URLs change.
    private const string UserFeedbackFormUrl   = "https://forms.gle/sX526E9QvarmPV2b7";
    private const string ExpertFeedbackFormUrl = "https://forms.gle/i3emZRNtFCCegHWT8";

    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramSessionLog> _sessionLogs;
    private readonly IRepository<User> _users;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EndEnrollmentCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public EndEnrollmentCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<ProgramSessionLog> sessionLogs,
        IRepository<User> users,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<EndEnrollmentCommandHandler> logger)
    {
        _access       = access;
        _experts      = experts;
        _sessionLogs  = sessionLogs;
        _users        = users;
        _emailService = emailService;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Ends the enrollment and notifies the enrolled user.</summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller is not authorised to end this enrollment.</exception>
    /// <exception cref="DomainException">Thrown when the enrollment is not in ACTIVE or PAUSED status.</exception>
    public async Task Handle(EndEnrollmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "EndEnrollment: user {UserId} (isAdmin={IsAdmin}, isUser={IsUser}) ending access {AccessId}",
            request.PerformedByUserId, request.IsAdmin, request.IsUser, request.AccessId);

        var record = await _access.FirstOrDefaultAsync(a => a.Id == request.AccessId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        // ── Authorisation ────────────────────────────────────────────────────
        string performedByRole;
        if (request.IsAdmin)
        {
            performedByRole = "ADMIN";
        }
        else if (request.IsUser)
        {
            if (record.UserId != request.PerformedByUserId)
                throw new ForbiddenException("You can only end your own enrollments.");
            performedByRole = "USER";
        }
        else
        {
            // Expert path
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.PerformedByUserId && !e.IsDeleted && e.IsActive, cancellationToken)
                ?? throw new ForbiddenException("You do not have an active expert profile.");

            if (expert.Id != record.ExpertId)
                throw new ForbiddenException("You can only end enrollments for your own programs.");

            performedByRole = "EXPERT";
        }

        // ── State guard ───────────────────────────────────────────────────────
        if (record.Status is not (UserProgramAccessStatus.Active or UserProgramAccessStatus.Paused))
            throw new DomainException($"Cannot end an enrollment that is currently {record.Status}. It must be ACTIVE or PAUSED.");

        // ── State transition ──────────────────────────────────────────────────
        var now = DateTimeOffset.UtcNow;
        record.Status      = UserProgramAccessStatus.Completed;
        record.CompletedAt = now;
        record.EndedBy     = request.PerformedByUserId;
        record.EndedByRole = performedByRole;
        record.UpdatedAt   = now;

        await _sessionLogs.AddAsync(new ProgramSessionLog
        {
            Id              = Guid.NewGuid(),
            AccessId        = record.Id,
            Action          = SessionAction.Ended,
            PerformedBy     = request.PerformedByUserId,
            PerformedByRole = performedByRole,
            Note            = request.Note,
            CreatedAt       = now
        });

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("EndEnrollment: access {AccessId} → COMPLETED", record.Id);

        // ── Notify enrolled user ──────────────────────────────────────────────
        await SendUserEmailAsync(record, "session_ended", cancellationToken);

        // ── Fix 12/13: Notify expert when user self-ends enrollment ───────────
        if (performedByRole == "USER")
            await SendExpertNotificationEmailAsync(record, cancellationToken);

        // ── Always send feedback form links on program end ────────────────────
        await SendFeedbackFormEmailsAsync(record, cancellationToken);
    }

    private async Task SendFeedbackFormEmailsAsync(
        UserProgramAccess record,
        CancellationToken cancellationToken)
    {
        // Enrolled user feedback
        try
        {
            var enrolledUser = await _users.FirstOrDefaultAsync(u => u.Id == record.UserId, cancellationToken);
            if (enrolledUser is not null)
            {
                var firstName = enrolledUser.FirstName;
                var html = $@"
<p>Hi {System.Net.WebUtility.HtmlEncode(firstName)},</p>
<p>Thank you for completing your guided program with FemVed. Your experience matters to us — would you mind taking a moment to share your feedback?</p>
<p><a href=""{UserFeedbackFormUrl}"">Open the feedback form</a></p>
<p>It only takes a couple of minutes and helps us keep improving the care we offer.</p>
<p>Warmly,<br/>The FemVed Team</p>";

                await _emailService.SendRawAsync(
                    toEmail:  enrolledUser.Email,
                    toName:   $"{enrolledUser.FirstName} {enrolledUser.LastName}",
                    subject:  "Share your feedback on your FemVed program",
                    htmlBody: html,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("EndEnrollment: user feedback-form email sent to {UserId}", enrolledUser.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EndEnrollment: failed to send user feedback-form email — enrollment update is still saved");
        }

        // Expert feedback
        try
        {
            var expertProfile = await _experts.FirstOrDefaultAsync(e => e.Id == record.ExpertId, cancellationToken);
            if (expertProfile is null) return;

            var expertUser = await _users.FirstOrDefaultAsync(u => u.Id == expertProfile.UserId, cancellationToken);
            if (expertUser is null) return;

            var html = $@"
<p>Hi {System.Net.WebUtility.HtmlEncode(expertUser.FirstName)},</p>
<p>One of your guided programs has just ended. We'd love a quick reflection from you to help shape FemVed's next chapter.</p>
<p><a href=""{ExpertFeedbackFormUrl}"">Open the expert feedback form</a></p>
<p>Thank you for the care you bring to our community.</p>
<p>Warmly,<br/>The FemVed Team</p>";

            await _emailService.SendRawAsync(
                toEmail:  expertUser.Email,
                toName:   $"{expertUser.FirstName} {expertUser.LastName}",
                subject:  "Share your feedback on the FemVed program you just ended",
                htmlBody: html,
                cancellationToken: cancellationToken);

            _logger.LogInformation("EndEnrollment: expert feedback-form email sent to {ExpertUserId}", expertUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EndEnrollment: failed to send expert feedback-form email — enrollment update is still saved");
        }
    }

    private async Task SendUserEmailAsync(
        UserProgramAccess record,
        string templateKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var enrolledUser = await _users.FirstOrDefaultAsync(u => u.Id == record.UserId, cancellationToken);
            if (enrolledUser is null) return;

            await _emailService.SendAsync(
                toEmail:      enrolledUser.Email,
                toName:       $"{enrolledUser.FirstName} {enrolledUser.LastName}",
                templateKey:  templateKey,
                templateData: new Dictionary<string, object>
                {
                    ["first_name"] = enrolledUser.FirstName
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("EndEnrollment: '{Template}' email sent to user {UserId}", templateKey, enrolledUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EndEnrollment: failed to send '{Template}' email — enrollment update is still saved", templateKey);
        }
    }

    private async Task SendExpertNotificationEmailAsync(
        UserProgramAccess record,
        CancellationToken cancellationToken)
    {
        try
        {
            var expertProfile = await _experts.FirstOrDefaultAsync(e => e.Id == record.ExpertId, cancellationToken);
            if (expertProfile is null) return;

            var expertUser = await _users.FirstOrDefaultAsync(u => u.Id == expertProfile.UserId, cancellationToken);
            if (expertUser is null) return;

            var enrolledUser = await _users.FirstOrDefaultAsync(u => u.Id == record.UserId, cancellationToken);
            var userName = enrolledUser is not null
                ? $"{enrolledUser.FirstName} {enrolledUser.LastName}"
                : "A user";

            await _emailService.SendAsync(
                toEmail:      expertUser.Email,
                toName:       $"{expertUser.FirstName} {expertUser.LastName}",
                templateKey:  "expert_enrollment_ended",
                templateData: new Dictionary<string, object>
                {
                    ["expert_first_name"] = expertUser.FirstName,
                    ["user_name"]         = userName
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("EndEnrollment: expert enrollment-ended notification sent to expert user {ExpertUserId}", expertUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EndEnrollment: failed to send expert notification email — enrollment update is still saved");
        }
    }
}
