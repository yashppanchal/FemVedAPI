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
