using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FemVed.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted background service that runs every 15 minutes and handles two jobs:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Auto-start</b>: finds <c>NotStarted</c> enrollments whose <c>ScheduledStartAt &lt;= UtcNow</c>,
///       transitions them to <c>Active</c>, writes a <c>program_session_log</c> row, and emails
///       the user (<c>session_started</c>), the expert (<c>session_started_expert</c>),
///       and admin (<c>admin_session_started</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>24h reminder</b>: finds <c>NotStarted</c> enrollments whose <c>ScheduledStartAt</c>
///       falls within the next 24 hours and where <c>StartReminderSentAt</c> is null,
///       sends a <c>session_start_reminder_24h</c> email to the user, and marks
///       <c>StartReminderSentAt = UtcNow</c> so the reminder is never duplicated.
///     </description>
///   </item>
/// </list>
/// Individual send failures are swallowed and logged but never abort the batch.
/// </summary>
public sealed class ScheduledProgramStartJob : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
    private static readonly string[] AdminEmails = { "aditi@femved.com", "femvedwellness@gmail.com" };
    private const string AdminName = "FemVed Admin";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledProgramStartJob> _logger;

    /// <summary>Initialises the job with a scope factory for creating scoped services each run.</summary>
    public ScheduledProgramStartJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledProgramStartJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    /// <summary>Loops indefinitely, running both checks every 15 minutes.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ScheduledProgramStartJob: started, checking every {Interval}", CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScheduledProgramStartJob: unhandled error during run");
            }
        }

        _logger.LogInformation("ScheduledProgramStartJob: stopping");
    }

    /// <summary>Performs a single run (auto-start + 24h reminder) within a fresh DI scope.</summary>
    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var accessRepo   = sp.GetRequiredService<IRepository<UserProgramAccess>>();
        var userRepo     = sp.GetRequiredService<IRepository<User>>();
        var expertRepo   = sp.GetRequiredService<IRepository<Expert>>();
        var programRepo  = sp.GetRequiredService<IRepository<Domain.Entities.Program>>();
        var sessionRepo  = sp.GetRequiredService<IRepository<ProgramSessionLog>>();
        var notifRepo    = sp.GetRequiredService<IRepository<NotificationLog>>();
        var emailService = sp.GetRequiredService<IEmailService>();
        var uow          = sp.GetRequiredService<IUnitOfWork>();

        var now = DateTimeOffset.UtcNow;

        await RunAutoStartAsync(accessRepo, userRepo, expertRepo, programRepo, sessionRepo, notifRepo, emailService, uow, now, ct);
        await Run24hReminderAsync(accessRepo, userRepo, programRepo, notifRepo, emailService, uow, now, ct);
    }

    // ── Auto-start ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds all NotStarted enrollments whose ScheduledStartAt has arrived,
    /// activates them, logs the action, and sends emails to all three parties.
    /// </summary>
    private async Task RunAutoStartAsync(
        IRepository<UserProgramAccess> accessRepo,
        IRepository<User> userRepo,
        IRepository<Expert> expertRepo,
        IRepository<Domain.Entities.Program> programRepo,
        IRepository<ProgramSessionLog> sessionRepo,
        IRepository<NotificationLog> notifRepo,
        IEmailService emailService,
        IUnitOfWork uow,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var due = await accessRepo.GetAllAsync(
            a => a.Status == UserProgramAccessStatus.NotStarted
              && a.ScheduledStartAt != null
              && a.ScheduledStartAt <= now,
            ct);

        if (due.Count == 0)
        {
            _logger.LogInformation("ScheduledProgramStartJob (auto-start): no records due at {Now}", now);
            return;
        }

        _logger.LogInformation(
            "ScheduledProgramStartJob (auto-start): {Count} record(s) to activate", due.Count);

        // Batch-load related entities
        var userIds    = due.Select(a => a.UserId).ToHashSet();
        var programIds = due.Select(a => a.ProgramId).ToHashSet();

        var users    = (await userRepo.GetAllAsync(u => userIds.Contains(u.Id) && !u.IsDeleted, ct)).ToDictionary(u => u.Id);
        var programs = (await programRepo.GetAllAsync(p => programIds.Contains(p.Id) && !p.IsDeleted, ct)).ToDictionary(p => p.Id);
        var expertIds = programs.Values.Select(p => p.ExpertId).ToHashSet();
        var experts  = (await expertRepo.GetAllAsync(e => expertIds.Contains(e.Id), ct)).ToDictionary(e => e.Id);

        var notifLogs   = new List<NotificationLog>();
        var sessionLogs = new List<ProgramSessionLog>();

        foreach (var access in due)
        {
            if (!users.TryGetValue(access.UserId, out var user)) continue;
            if (!programs.TryGetValue(access.ProgramId, out var program)) continue;
            experts.TryGetValue(program.ExpertId, out var expert);

            // Activate
            access.Status         = UserProgramAccessStatus.Active;
            access.StartedAt      = now;
            access.ScheduledStartAt = null;
            access.UpdatedAt      = now;
            accessRepo.Update(access);

            sessionLogs.Add(new ProgramSessionLog
            {
                Id              = Guid.NewGuid(),
                AccessId        = access.Id,
                Action          = SessionAction.Started,
                PerformedBy     = null,
                PerformedByRole = "SYSTEM",
                Note            = "Auto-started by scheduled job",
                CreatedAt       = now
            });

            var programName = program.Name ?? string.Empty;
            var userName    = $"{user.FirstName} {user.LastName}";
            var expertName  = expert?.DisplayName ?? string.Empty;

            // Email: user
            await TrySendEmailAsync(emailService, notifRepo, notifLogs, user.Email, userName,
                "session_started",
                new Dictionary<string, object>
                {
                    ["first_name"]   = user.FirstName,
                    ["program_name"] = programName
                },
                user.Id, ct);

            // Email: expert
            if (expert is not null)
            {
                var expertUser = users.Values.FirstOrDefault(u => u.Id == expert.UserId);
                if (expertUser is not null)
                {
                    await TrySendEmailAsync(emailService, notifRepo, notifLogs,
                        expertUser.Email, expertName,
                        "session_started_expert",
                        new Dictionary<string, object>
                        {
                            ["expert_name"]  = expertName,
                            ["user_name"]    = userName,
                            ["user_email"]   = user.Email,
                            ["program_name"] = programName
                        },
                        expertUser.Id, ct);
                }
            }

            // Email: all admins
            var adminStartedData = new Dictionary<string, object>
            {
                ["user_name"]    = userName,
                ["user_email"]   = user.Email,
                ["expert_name"]  = expertName,
                ["program_name"] = programName,
                ["access_id"]    = access.Id.ToString()
            };
            foreach (var adminEmail in AdminEmails)
                await TrySendEmailAsync(emailService, notifRepo, notifLogs,
                    adminEmail, AdminName, "admin_session_started", adminStartedData, null, ct);
        }

        foreach (var log in sessionLogs) await sessionRepo.AddAsync(log);
        foreach (var log in notifLogs)   await notifRepo.AddAsync(log);

        try
        {
            await uow.SaveChangesAsync(ct);
            _logger.LogInformation(
                "ScheduledProgramStartJob (auto-start): activated {Count} enrollment(s)", due.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScheduledProgramStartJob (auto-start): failed to persist changes");
        }
    }

    // ── 24h reminder ───────────────────────────────────────────────────────────

    /// <summary>
    /// Finds NotStarted enrollments whose ScheduledStartAt is within the next 24 hours
    /// and where the 24h reminder has not yet been sent.
    /// Sends a <c>session_start_reminder_24h</c> email to the user and marks <c>StartReminderSentAt</c>.
    /// </summary>
    private async Task Run24hReminderAsync(
        IRepository<UserProgramAccess> accessRepo,
        IRepository<User> userRepo,
        IRepository<Domain.Entities.Program> programRepo,
        IRepository<NotificationLog> notifRepo,
        IEmailService emailService,
        IUnitOfWork uow,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var cutoff = now.AddHours(24);

        var upcoming = await accessRepo.GetAllAsync(
            a => a.Status == UserProgramAccessStatus.NotStarted
              && a.ScheduledStartAt != null
              && a.ScheduledStartAt > now
              && a.ScheduledStartAt <= cutoff
              && a.StartReminderSentAt == null,
            ct);

        if (upcoming.Count == 0)
        {
            _logger.LogInformation(
                "ScheduledProgramStartJob (24h reminder): no enrollments needing reminder at {Now}", now);
            return;
        }

        _logger.LogInformation(
            "ScheduledProgramStartJob (24h reminder): {Count} reminder(s) to send", upcoming.Count);

        var userIds    = upcoming.Select(a => a.UserId).ToHashSet();
        var programIds = upcoming.Select(a => a.ProgramId).ToHashSet();

        var users    = (await userRepo.GetAllAsync(u => userIds.Contains(u.Id) && !u.IsDeleted, ct)).ToDictionary(u => u.Id);
        var programs = (await programRepo.GetAllAsync(p => programIds.Contains(p.Id) && !p.IsDeleted, ct)).ToDictionary(p => p.Id);

        var notifLogs = new List<NotificationLog>();

        foreach (var access in upcoming)
        {
            if (!users.TryGetValue(access.UserId, out var user)) continue;
            if (!programs.TryGetValue(access.ProgramId, out var program)) continue;

            var startLabel = access.ScheduledStartAt!.Value.ToString("MMMM d, yyyy");

            await TrySendEmailAsync(emailService, notifRepo, notifLogs,
                user.Email, $"{user.FirstName} {user.LastName}",
                "session_start_reminder_24h",
                new Dictionary<string, object>
                {
                    ["first_name"]   = user.FirstName,
                    ["program_name"] = program.Name ?? string.Empty,
                    ["start_date"]   = startLabel
                },
                user.Id, ct);

            access.StartReminderSentAt = now;
            access.UpdatedAt           = now;
            accessRepo.Update(access);
        }

        foreach (var log in notifLogs) await notifRepo.AddAsync(log);

        try
        {
            await uow.SaveChangesAsync(ct);
            _logger.LogInformation(
                "ScheduledProgramStartJob (24h reminder): sent {Count} reminder(s)", upcoming.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScheduledProgramStartJob (24h reminder): failed to persist reminder state");
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to send an email and appends a <see cref="NotificationLog"/> to <paramref name="logs"/>.
    /// Exceptions are swallowed and logged — they never abort the batch.
    /// </summary>
    private async Task TrySendEmailAsync(
        IEmailService emailService,
        IRepository<NotificationLog> notifRepo,
        List<NotificationLog> logs,
        string toEmail,
        string toName,
        string templateKey,
        Dictionary<string, object> templateData,
        Guid? userId,
        CancellationToken ct)
    {
        var status   = NotificationStatus.Sent;
        string? error = null;

        try
        {
            await emailService.SendAsync(toEmail, toName, templateKey, templateData, ct);
        }
        catch (Exception ex)
        {
            status = NotificationStatus.Failed;
            error  = ex.Message;
            _logger.LogError(ex,
                "ScheduledProgramStartJob: failed to send {Template} to {Email}", templateKey, toEmail);
        }

        logs.Add(new NotificationLog
        {
            Id           = Guid.NewGuid(),
            UserId       = userId ?? Guid.Empty,
            Type         = NotificationType.Email,
            TemplateKey  = templateKey,
            Recipient    = toEmail,
            Status       = status,
            ErrorMessage = error,
            Payload      = JsonSerializer.Serialize(templateData),
            CreatedAt    = DateTimeOffset.UtcNow
        });
    }
}
