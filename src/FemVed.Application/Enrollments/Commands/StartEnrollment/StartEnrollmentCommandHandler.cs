using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Enrollments.Commands.StartEnrollment;

/// <summary>
/// Handles <see cref="StartEnrollmentCommand"/>.
/// <list type="number">
///   <item>Verifies the enrollment exists and the caller is authorised to start it.</item>
///   <item>Guards against invalid state transitions (must be NOT_STARTED).</item>
///   <item>
///     If <c>ScheduledDate</c> is null or today → sets Status = ACTIVE, StartedAt = now,
///     clears ScheduledStartAt, and emails user + expert + admin with <c>session_started</c> templates.
///   </item>
///   <item>
///     If <c>ScheduledDate</c> is a future date → stores ScheduledStartAt, keeps Status = NOT_STARTED,
///     and emails user + expert + admin with <c>session_scheduled</c> templates.
///   </item>
/// </list>
/// </summary>
public sealed class StartEnrollmentCommandHandler : IRequestHandler<StartEnrollmentCommand>
{
    private static readonly string[] AdminEmails = { "aditi@femved.com", "femvedwellness@gmail.com" };
    private const string AdminName = "FemVed Admin";

    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramSessionLog> _sessionLogs;
    private readonly IRepository<User> _users;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<StartEnrollmentCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public StartEnrollmentCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<ProgramSessionLog> sessionLogs,
        IRepository<User> users,
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<StartEnrollmentCommandHandler> logger)
    {
        _access       = access;
        _experts      = experts;
        _sessionLogs  = sessionLogs;
        _users        = users;
        _programs     = programs;
        _durations    = durations;
        _emailService = emailService;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Starts or schedules the enrollment, then notifies all parties.</summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller is not the expert for this program.</exception>
    /// <exception cref="DomainException">Thrown when the enrollment is not in NOT_STARTED status.</exception>
    public async Task Handle(StartEnrollmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "StartEnrollment: user {UserId} (isAdmin={IsAdmin}) starting access {AccessId} (scheduledDate={ScheduledDate})",
            request.PerformedByUserId, request.IsAdmin, request.AccessId, request.ScheduledDate);

        var record = await _access.FirstOrDefaultAsync(a => a.Id == request.AccessId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        // ── Authorisation ────────────────────────────────────────────────────
        string performedByRole;
        if (request.IsAdmin)
        {
            performedByRole = "ADMIN";
        }
        else
        {
            var callerExpert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.PerformedByUserId && !e.IsDeleted && e.IsActive, cancellationToken)
                ?? throw new ForbiddenException("You do not have an active expert profile.");

            if (callerExpert.Id != record.ExpertId)
                throw new ForbiddenException("You can only start enrollments for your own programs.");

            performedByRole = "EXPERT";
        }

        // ── State guard ───────────────────────────────────────────────────────
        if (record.Status != UserProgramAccessStatus.NotStarted)
            throw new DomainException($"Cannot start an enrollment that is currently {record.Status}. It must be NOT_STARTED.");

        var now         = DateTimeOffset.UtcNow;
        var today       = DateOnly.FromDateTime(DateTime.UtcNow);
        var isScheduled = request.ScheduledDate.HasValue && request.ScheduledDate.Value > today;

        // ── Load related data for emails + duration ───────────────────────────
        var enrolledUser = await _users.FirstOrDefaultAsync(u => u.Id == record.UserId, cancellationToken);
        var program      = await _programs.FirstOrDefaultAsync(p => p.Id == record.ProgramId, cancellationToken);
        var expert       = await _experts.FirstOrDefaultAsync(e => e.Id == record.ExpertId, cancellationToken);
        var duration     = await _durations.FirstOrDefaultAsync(d => d.Id == record.DurationId, cancellationToken);
        User? expertUser = null;
        if (expert is not null)
            expertUser = await _users.FirstOrDefaultAsync(u => u.Id == expert.UserId, cancellationToken);

        var programName = program?.Name ?? "your program";
        var expertName  = expert?.DisplayName ?? "your expert";
        var userName    = enrolledUser is not null ? $"{enrolledUser.FirstName} {enrolledUser.LastName}" : "the user";

        if (isScheduled)
        {
            // ── Future scheduled start ────────────────────────────────────────
            var scheduledDate    = request.ScheduledDate!.Value;
            var scheduledDateUtc = new DateTimeOffset(scheduledDate.Year, scheduledDate.Month, scheduledDate.Day, 0, 0, 0, TimeSpan.Zero);
            var startDateLabel   = scheduledDate.ToString("MMMM d, yyyy");

            record.ScheduledStartAt = scheduledDateUtc;
            record.UpdatedAt        = now;

            await _sessionLogs.AddAsync(new ProgramSessionLog
            {
                Id              = Guid.NewGuid(),
                AccessId        = record.Id,
                Action          = SessionAction.Scheduled,
                PerformedBy     = request.PerformedByUserId,
                PerformedByRole = performedByRole,
                Note            = request.Note ?? $"Scheduled for {startDateLabel}",
                CreatedAt       = now
            });

            await _uow.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("StartEnrollment: access {AccessId} scheduled for {Date}", record.Id, scheduledDate);

            if (enrolledUser is not null)
                await TrySendEmailAsync(enrolledUser.Email, $"{enrolledUser.FirstName} {enrolledUser.LastName}",
                    "session_scheduled",
                    new Dictionary<string, object>
                    {
                        ["first_name"]   = enrolledUser.FirstName,
                        ["program_name"] = programName,
                        ["start_date"]   = startDateLabel
                    }, cancellationToken);

            if (expertUser is not null)
                await TrySendEmailAsync(expertUser.Email, $"{expertUser.FirstName} {expertUser.LastName}",
                    "session_scheduled_expert",
                    new Dictionary<string, object>
                    {
                        ["expert_first_name"] = expertUser.FirstName,
                        ["user_name"]         = userName,
                        ["program_name"]      = programName,
                        ["start_date"]        = startDateLabel
                    }, cancellationToken);

            foreach (var adminEmail in AdminEmails)
                await TrySendEmailAsync(adminEmail, AdminName, "admin_session_scheduled",
                    new Dictionary<string, object>
                    {
                        ["user_name"]    = userName,
                        ["expert_name"]  = expertName,
                        ["program_name"] = programName,
                        ["start_date"]   = startDateLabel
                    }, cancellationToken);
        }
        else
        {
            // ── Immediate start ───────────────────────────────────────────────
            var weeksCount = duration?.Weeks ?? 0;
            record.Status           = UserProgramAccessStatus.Active;
            record.StartedAt        = now;
            record.ScheduledStartAt = null;
            record.EndDate          = weeksCount > 0 ? now.AddDays(weeksCount * 7) : null;
            record.UpdatedAt        = now;

            await _sessionLogs.AddAsync(new ProgramSessionLog
            {
                Id              = Guid.NewGuid(),
                AccessId        = record.Id,
                Action          = SessionAction.Started,
                PerformedBy     = request.PerformedByUserId,
                PerformedByRole = performedByRole,
                Note            = request.Note,
                CreatedAt       = now
            });

            await _uow.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("StartEnrollment: access {AccessId} → ACTIVE", record.Id);

            if (enrolledUser is not null)
                await TrySendEmailAsync(enrolledUser.Email, $"{enrolledUser.FirstName} {enrolledUser.LastName}",
                    "session_started",
                    new Dictionary<string, object>
                    {
                        ["first_name"]   = enrolledUser.FirstName,
                        ["program_name"] = programName
                    }, cancellationToken);

            if (expertUser is not null)
                await TrySendEmailAsync(expertUser.Email, $"{expertUser.FirstName} {expertUser.LastName}",
                    "session_started_expert",
                    new Dictionary<string, object>
                    {
                        ["expert_first_name"] = expertUser.FirstName,
                        ["user_name"]         = userName,
                        ["user_email"]        = enrolledUser?.Email ?? string.Empty,
                        ["program_name"]      = programName
                    }, cancellationToken);

            foreach (var adminEmail in AdminEmails)
                await TrySendEmailAsync(adminEmail, AdminName, "admin_session_started",
                    new Dictionary<string, object>
                    {
                        ["user_name"]    = userName,
                        ["user_email"]   = enrolledUser?.Email ?? string.Empty,
                        ["expert_name"]  = expertName,
                        ["program_name"] = programName
                    }, cancellationToken);
        }
    }

    /// <summary>Sends an email, logging any failure without propagating it.</summary>
    private async Task TrySendEmailAsync(
        string toEmail,
        string toName,
        string templateKey,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailService.SendAsync(toEmail, toName, templateKey, templateData, cancellationToken);
            _logger.LogInformation("StartEnrollment: '{Template}' email sent to {Email}", templateKey, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "StartEnrollment: failed to send '{Template}' email to {Email} — enrollment update is still saved",
                templateKey, toEmail);
        }
    }
}
