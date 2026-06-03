using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Enrollments.Commands.EndEnrollment;

/// <summary>
/// Handles <see cref="EndEnrollmentCommand"/>.
/// <list type="number">
///   <item>Verifies the enrollment exists and the caller is authorised to end it.</item>
///   <item>Guards against invalid state transitions (must be ACTIVE or PAUSED).</item>
///   <item>Sets Status = COMPLETED, CompletedAt = now, EndedBy + EndedByRole.</item>
///   <item>Appends a <see cref="ProgramSessionLog"/> entry.</item>
///   <item>Sends one consolidated <c>program_ended_user</c> email to the enrolled user (with the user feedback form).</item>
///   <item>Sends one <c>program_ended_expert_admin</c> email to the expert and to each admin in <c>ADMIN_NOTIFICATION_EMAILS</c> (with the expert feedback form).</item>
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
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<EndEnrollmentCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public EndEnrollmentCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<ProgramSessionLog> sessionLogs,
        IRepository<User> users,
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IEmailService emailService,
        IConfiguration configuration,
        IUnitOfWork uow,
        ILogger<EndEnrollmentCommandHandler> logger)
    {
        _access        = access;
        _experts       = experts;
        _sessionLogs   = sessionLogs;
        _users         = users;
        _programs      = programs;
        _durations     = durations;
        _emailService  = emailService;
        _configuration = configuration;
        _uow           = uow;
        _logger        = logger;
    }

    /// <summary>Ends the enrollment and notifies the enrolled user, the expert, and admins.</summary>
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

        // ── Send consolidated emails ──────────────────────────────────────────
        await SendProgramEndedEmailsAsync(record, performedByRole, now, cancellationToken);
    }

    /// <summary>
    /// Loads program / expert / user context once and sends:
    /// (1) a single email to the enrolled user with the user feedback form,
    /// (2) a single email to the expert and to each configured admin with the expert feedback form.
    /// All sends are wrapped in try/catch — the enrollment state change is already persisted.
    /// </summary>
    private async Task SendProgramEndedEmailsAsync(
        UserProgramAccess record,
        string performedByRole,
        DateTimeOffset endedAt,
        CancellationToken cancellationToken)
    {
        // Load everything we need for the email body
        var enrolledUser = await _users.FirstOrDefaultAsync(u => u.Id == record.UserId, cancellationToken);
        var expert       = await _experts.FirstOrDefaultAsync(e => e.Id == record.ExpertId, cancellationToken);
        var expertUser   = expert is not null
            ? await _users.FirstOrDefaultAsync(u => u.Id == expert.UserId, cancellationToken)
            : null;
        var program      = await _programs.FirstOrDefaultAsync(p => p.Id == record.ProgramId, cancellationToken);
        var duration     = await _durations.FirstOrDefaultAsync(d => d.Id == record.DurationId, cancellationToken);

        var programName   = program?.Name ?? "Your Program";
        var durationLabel = duration?.Label ?? string.Empty;
        var expertName    = !string.IsNullOrWhiteSpace(expert?.DisplayName)
            ? expert!.DisplayName
            : expertUser is not null ? $"{expertUser.FirstName} {expertUser.LastName}" : "Your Expert";
        var expertTitle   = expert?.Title ?? string.Empty;
        var userName      = enrolledUser is not null
            ? $"{enrolledUser.FirstName} {enrolledUser.LastName}"
            : "A user";
        var endedOn       = endedAt.ToString("dddd, d MMMM yyyy");
        var year          = endedAt.Year.ToString();
        var appBaseUrl    = _configuration["APP_BASE_URL"] ?? "https://femved.com";
        var dashboardUrl  = $"{appBaseUrl}/dashboard";

        // ── 1. Consolidated email to enrolled user ───────────────────────────
        if (enrolledUser is not null)
        {
            try
            {
                await _emailService.SendAsync(
                    toEmail:      enrolledUser.Email,
                    toName:       $"{enrolledUser.FirstName} {enrolledUser.LastName}",
                    templateKey:  "program_ended_user",
                    templateData: new Dictionary<string, object>
                    {
                        ["firstName"]     = enrolledUser.FirstName,
                        ["programName"]   = programName,
                        ["expertName"]    = expertName,
                        ["expertTitle"]   = expertTitle,
                        ["durationLabel"] = durationLabel,
                        ["endedOn"]       = endedOn,
                        ["endedBy"]       = performedByRole,
                        ["feedbackUrl"]   = UserFeedbackFormUrl,
                        ["dashboardUrl"]  = dashboardUrl,
                        ["year"]          = year
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("EndEnrollment: program_ended_user email sent to {UserId}", enrolledUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EndEnrollment: failed to send program_ended_user email — enrollment update is still saved");
            }
        }

        // ── 2. Consolidated email to expert (same template, expert form) ─────
        if (expertUser is not null)
        {
            await SendExpertAdminEmailAsync(
                toEmail:            expertUser.Email,
                toName:             $"{expertUser.FirstName} {expertUser.LastName}",
                recipientFirstName: expertUser.FirstName,
                userName:           userName,
                userEmail:          enrolledUser?.Email ?? string.Empty,
                userMobile:         enrolledUser?.FullMobile ?? string.Empty,
                programName:        programName,
                expertName:         expertName,
                expertTitle:        expertTitle,
                durationLabel:      durationLabel,
                endedOn:            endedOn,
                endedBy:            performedByRole,
                year:               year,
                cancellationToken:  cancellationToken);
        }

        // ── 3. Same email to each admin in ADMIN_NOTIFICATION_EMAILS ─────────
        var adminEmails = (_configuration["ADMIN_NOTIFICATION_EMAILS"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var adminEmail in adminEmails)
        {
            await SendExpertAdminEmailAsync(
                toEmail:            adminEmail,
                toName:             "FemVed Admin",
                recipientFirstName: "FemVed Admin",
                userName:           userName,
                userEmail:          enrolledUser?.Email ?? string.Empty,
                userMobile:         enrolledUser?.FullMobile ?? string.Empty,
                programName:        programName,
                expertName:         expertName,
                expertTitle:        expertTitle,
                durationLabel:      durationLabel,
                endedOn:            endedOn,
                endedBy:            performedByRole,
                year:               year,
                cancellationToken:  cancellationToken);
        }
    }

    private async Task SendExpertAdminEmailAsync(
        string toEmail,
        string toName,
        string recipientFirstName,
        string userName,
        string userEmail,
        string userMobile,
        string programName,
        string expertName,
        string expertTitle,
        string durationLabel,
        string endedOn,
        string endedBy,
        string year,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(
                toEmail:      toEmail,
                toName:       toName,
                templateKey:  "program_ended_expert_admin",
                templateData: new Dictionary<string, object>
                {
                    ["recipientFirstName"] = recipientFirstName,
                    ["userName"]           = userName,
                    ["userEmail"]          = userEmail,
                    ["userMobile"]         = userMobile,
                    ["programName"]        = programName,
                    ["expertName"]         = expertName,
                    ["expertTitle"]        = expertTitle,
                    ["durationLabel"]      = durationLabel,
                    ["endedOn"]            = endedOn,
                    ["endedBy"]            = endedBy,
                    ["feedbackUrl"]        = ExpertFeedbackFormUrl,
                    ["year"]               = year
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("EndEnrollment: program_ended_expert_admin email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EndEnrollment: failed to send program_ended_expert_admin email to {Email} — enrollment update is still saved", toEmail);
        }
    }
}
