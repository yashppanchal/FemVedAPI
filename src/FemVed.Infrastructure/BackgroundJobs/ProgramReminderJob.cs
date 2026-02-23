using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FemVed.Infrastructure.BackgroundJobs;

/// <summary>
/// Hosted background service that runs every hour and sends 24-hour program-start reminders
/// to enrolled users whose program begins the following calendar day (UTC).
/// Sets <c>UserProgramAccess.ReminderSent = true</c> after dispatching so reminders are never duplicated.
/// Notifications are logged to <c>notification_log</c>; individual send failures never abort the batch.
/// </summary>
public sealed class ProgramReminderJob : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProgramReminderJob> _logger;

    /// <summary>Initialises the job with a scope factory for creating scoped services each run.</summary>
    public ProgramReminderJob(IServiceScopeFactory scopeFactory, ILogger<ProgramReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    /// <summary>Loops indefinitely, running the reminder check every hour.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProgramReminderJob: started, checking every {Interval}", CheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProgramReminderJob: unhandled error during reminder run");
            }
        }

        _logger.LogInformation("ProgramReminderJob: stopping");
    }

    /// <summary>Performs a single reminder run within a fresh DI scope.</summary>
    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var programRepo  = sp.GetRequiredService<IRepository<Domain.Entities.Program>>();
        var accessRepo   = sp.GetRequiredService<IRepository<UserProgramAccess>>();
        var userRepo     = sp.GetRequiredService<IRepository<User>>();
        var notifRepo    = sp.GetRequiredService<IRepository<NotificationLog>>();
        var emailService = sp.GetRequiredService<IEmailService>();
        var waService    = sp.GetRequiredService<IWhatsAppService>();
        var uow          = sp.GetRequiredService<IUnitOfWork>();

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        _logger.LogInformation("ProgramReminderJob: checking for programs starting on {Date}", tomorrow);

        // ── Step 1: programs starting tomorrow ────────────────────────────────
        var programs = await programRepo.GetAllAsync(
            p => p.StartDate == tomorrow && !p.IsDeleted && p.Status == ProgramStatus.Published, ct);

        if (programs.Count == 0)
        {
            _logger.LogInformation("ProgramReminderJob: no programs start on {Date}", tomorrow);
            return;
        }

        var programIds = programs.Select(p => p.Id).ToHashSet();
        var programMap = programs.ToDictionary(p => p.Id);

        // ── Step 2: access records that haven't been reminded yet ─────────────
        var accessRecords = await accessRepo.GetAllAsync(
            a => programIds.Contains(a.ProgramId)
              && !a.ReminderSent
              && a.Status == UserProgramAccessStatus.Active,
            ct);

        if (accessRecords.Count == 0)
        {
            _logger.LogInformation("ProgramReminderJob: all users already reminded for programs on {Date}", tomorrow);
            return;
        }

        _logger.LogInformation("ProgramReminderJob: sending reminders for {Count} enrollments on {Date}",
            accessRecords.Count, tomorrow);

        // ── Step 3: batch-load users ──────────────────────────────────────────
        var userIds = accessRecords.Select(a => a.UserId).ToHashSet();
        var users   = await userRepo.GetAllAsync(u => userIds.Contains(u.Id) && !u.IsDeleted, ct);
        var userMap = users.ToDictionary(u => u.Id);

        var startDateLabel = tomorrow.ToString("MMMM d, yyyy");
        var notifLogs      = new List<NotificationLog>();

        // ── Step 4: send notifications per access record ──────────────────────
        foreach (var access in accessRecords)
        {
            if (!userMap.TryGetValue(access.UserId, out var user)) continue;
            if (!programMap.TryGetValue(access.ProgramId, out var program)) continue;

            var templateData = new Dictionary<string, object>
            {
                ["firstName"]   = user.FirstName,
                ["programName"] = program.Name,
                ["startDate"]   = startDateLabel
            };

            // Email reminder
            var emailStatus = NotificationStatus.Sent;
            string? emailError = null;
            try
            {
                await emailService.SendAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    "program_reminder",
                    templateData,
                    ct);
            }
            catch (Exception ex)
            {
                emailStatus = NotificationStatus.Failed;
                emailError  = ex.Message;
                _logger.LogWarning(ex, "ProgramReminderJob: email reminder failed for user {UserId}", user.Id);
            }

            notifLogs.Add(new NotificationLog
            {
                Id           = Guid.NewGuid(),
                UserId       = user.Id,
                Type         = NotificationType.Email,
                TemplateKey  = "program_reminder",
                Recipient    = user.Email,
                Status       = emailStatus,
                ErrorMessage = emailError,
                CreatedAt    = DateTimeOffset.UtcNow
            });

            // WhatsApp reminder (only if opted in and number is set)
            if (user.WhatsAppOptIn && !string.IsNullOrEmpty(user.FullMobile))
            {
                var waStatus = NotificationStatus.Sent;
                string? waError = null;
                try
                {
                    await waService.SendAsync(
                        user.FullMobile,
                        "program_reminder_wa",
                        [user.FirstName, program.Name, startDateLabel],
                        ct);
                }
                catch (Exception ex)
                {
                    waStatus = NotificationStatus.Failed;
                    waError  = ex.Message;
                    _logger.LogWarning(ex, "ProgramReminderJob: WhatsApp reminder failed for user {UserId}", user.Id);
                }

                notifLogs.Add(new NotificationLog
                {
                    Id           = Guid.NewGuid(),
                    UserId       = user.Id,
                    Type         = NotificationType.WhatsApp,
                    TemplateKey  = "program_reminder_wa",
                    Recipient    = user.FullMobile,
                    Status       = waStatus,
                    ErrorMessage = waError,
                    CreatedAt    = DateTimeOffset.UtcNow
                });
            }

            // Mark reminder sent regardless of notification success
            access.ReminderSent = true;
            access.UpdatedAt    = DateTimeOffset.UtcNow;
            accessRepo.Update(access);
        }

        // ── Step 5: persist notification logs + updated access records ────────
        foreach (var log in notifLogs)
            await notifRepo.AddAsync(log);

        try
        {
            await uow.SaveChangesAsync(ct);
            _logger.LogInformation("ProgramReminderJob: completed {Count} reminder(s) for {Date}",
                accessRecords.Count, tomorrow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProgramReminderJob: failed to persist reminder state for {Date}", tomorrow);
        }
    }
}
