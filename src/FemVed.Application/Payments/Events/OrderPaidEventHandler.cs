using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Events;

/// <summary>
/// Handles <see cref="OrderPaidEvent"/>.
/// Post-payment responsibilities (in order):
/// <list type="number">
///   <item>Create a <see cref="UserProgramAccess"/> record so the user can access the program (idempotent).</item>
///   <item>Send <c>purchase_success</c> email to the user via SendGrid.</item>
///   <item>Send <c>purchase_confirmation_wa</c> WhatsApp message to the user (if opted in and globally enabled).</item>
///   <item>Send <c>expert_new_enrollment</c> email to the expert via SendGrid.</item>
///   <item>Log every notification attempt (Sent / Failed) to <see cref="NotificationLog"/>.</item>
/// </list>
/// Notification failures are caught and recorded — they never propagate and never roll back the payment.
/// </summary>
public sealed class OrderPaidEventHandler : INotificationHandler<OrderPaidEvent>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<User> _users;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<NotificationLog> _notificationLogs;
    private readonly IEmailService _emailService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderPaidEventHandler> _logger;

    /// <summary>Initialises the handler with all required services.</summary>
    public OrderPaidEventHandler(
        IRepository<UserProgramAccess> access,
        IRepository<User> users,
        IRepository<Expert> experts,
        IRepository<NotificationLog> notificationLogs,
        IEmailService emailService,
        IWhatsAppService whatsAppService,
        IUnitOfWork uow,
        ILogger<OrderPaidEventHandler> logger)
    {
        _access           = access;
        _users            = users;
        _experts          = experts;
        _notificationLogs = notificationLogs;
        _emailService     = emailService;
        _whatsAppService  = whatsAppService;
        _uow              = uow;
        _logger           = logger;
    }

    /// <summary>Creates program access and triggers post-purchase notifications.</summary>
    /// <param name="notification">The domain event payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(OrderPaidEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "OrderPaidEvent received: Order={OrderId}, User={UserId}, Program={ProgramId}, Expert={ExpertId}",
            notification.OrderId, notification.UserId, notification.ProgramId, notification.ExpertId);

        // ── 1. Create UserProgramAccess (idempotent) ─────────────────────────
        await EnsureUserProgramAccessAsync(notification, cancellationToken);

        // ── Load recipients ──────────────────────────────────────────────────
        var user   = await _users.FirstOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);
        var expert = await _experts.FirstOrDefaultAsync(e => e.Id == notification.ExpertId, cancellationToken);

        User? expertUser = null;
        if (expert is not null)
        {
            expertUser = await _users.FirstOrDefaultAsync(u => u.Id == expert.UserId, cancellationToken);
        }

        // ── 2. purchase_success email → user ─────────────────────────────────
        if (user is not null)
        {
            var userEmailData = new Dictionary<string, object>
            {
                ["first_name"] = user.FirstName,
                ["order_id"]   = notification.OrderId.ToString()
            };

            await SendEmailWithLogAsync(
                toEmail:     user.Email,
                toName:      $"{user.FirstName} {user.LastName}",
                templateKey: "purchase_success",
                templateData: userEmailData,
                userId:      user.Id,
                cancellationToken: cancellationToken);
        }
        else
        {
            _logger.LogWarning("OrderPaidEventHandler: user {UserId} not found, skipping purchase_success email", notification.UserId);
        }

        // ── 3. purchase_confirmation_wa WhatsApp → user (if opted in) ────────
        if (user is not null && user.WhatsAppOptIn && !string.IsNullOrEmpty(user.FullMobile))
        {
            await SendWhatsAppWithLogAsync(
                toNumber:      user.FullMobile,
                templateName:  "purchase_confirmation_wa",
                templateParams: new[] { user.FirstName, notification.OrderId.ToString() },
                userId:        user.Id,
                cancellationToken: cancellationToken);
        }

        // ── 4. expert_new_enrollment email → expert's user account ──────────
        if (expertUser is not null)
        {
            var expertEmailData = new Dictionary<string, object>
            {
                ["expert_first_name"] = expertUser.FirstName,
                ["order_id"]          = notification.OrderId.ToString(),
                ["program_id"]        = notification.ProgramId.ToString()
            };

            await SendEmailWithLogAsync(
                toEmail:     expertUser.Email,
                toName:      $"{expertUser.FirstName} {expertUser.LastName}",
                templateKey: "expert_new_enrollment",
                templateData: expertEmailData,
                userId:      expertUser.Id,
                cancellationToken: cancellationToken);
        }
        else
        {
            _logger.LogWarning("OrderPaidEventHandler: expert user not found for expert {ExpertId}, skipping expert_new_enrollment email", notification.ExpertId);
        }

        _logger.LogInformation("OrderPaidEvent handled successfully for order {OrderId}", notification.OrderId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a <see cref="UserProgramAccess"/> record if one does not already exist for the order.</summary>
    private async Task EnsureUserProgramAccessAsync(OrderPaidEvent notification, CancellationToken cancellationToken)
    {
        var existing = await _access.FirstOrDefaultAsync(
            a => a.OrderId == notification.OrderId, cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation("UserProgramAccess already exists for order {OrderId} — skipping", notification.OrderId);
            return;
        }

        var record = new UserProgramAccess
        {
            Id        = Guid.NewGuid(),
            UserId    = notification.UserId,
            OrderId   = notification.OrderId,
            ProgramId = notification.ProgramId,
            DurationId = notification.DurationId,
            ExpertId  = notification.ExpertId,
            Status    = UserProgramAccessStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _access.AddAsync(record);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UserProgramAccess created for user {UserId}, program {ProgramId}",
            notification.UserId, notification.ProgramId);
    }

    /// <summary>
    /// Sends an email via <see cref="IEmailService"/> and persists a <see cref="NotificationLog"/> entry
    /// regardless of whether the send succeeds or fails.
    /// </summary>
    private async Task SendEmailWithLogAsync(
        string toEmail,
        string toName,
        string templateKey,
        Dictionary<string, object> templateData,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var status       = NotificationStatus.Sent;
        string? errorMsg = null;

        try
        {
            await _emailService.SendAsync(toEmail, toName, templateKey, templateData, cancellationToken);
            _logger.LogInformation("Email '{TemplateKey}' sent to {Email}", templateKey, toEmail);
        }
        catch (Exception ex)
        {
            status   = NotificationStatus.Failed;
            errorMsg = ex.Message;
            _logger.LogError(ex, "Failed to send email '{TemplateKey}' to {Email}", templateKey, toEmail);
        }

        await PersistNotificationLogAsync(
            userId:      userId,
            type:        NotificationType.Email,
            templateKey: templateKey,
            recipient:   toEmail,
            status:      status,
            errorMsg:    errorMsg,
            payload:     templateData,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a WhatsApp message via <see cref="IWhatsAppService"/> and persists a <see cref="NotificationLog"/> entry.
    /// The global toggle (<c>WHATSAPP_ENABLED</c>) is enforced inside <see cref="IWhatsAppService"/> — this method
    /// only calls the service when the per-user opt-in is already confirmed by the caller.
    /// </summary>
    private async Task SendWhatsAppWithLogAsync(
        string toNumber,
        string templateName,
        IEnumerable<string> templateParams,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var status       = NotificationStatus.Sent;
        string? errorMsg = null;

        try
        {
            await _whatsAppService.SendAsync(toNumber, templateName, templateParams, cancellationToken);
            _logger.LogInformation("WhatsApp '{TemplateName}' sent to {Number}", templateName, toNumber);
        }
        catch (Exception ex)
        {
            status   = NotificationStatus.Failed;
            errorMsg = ex.Message;
            _logger.LogError(ex, "Failed to send WhatsApp '{TemplateName}' to {Number}", templateName, toNumber);
        }

        await PersistNotificationLogAsync(
            userId:      userId,
            type:        NotificationType.WhatsApp,
            templateKey: templateName,
            recipient:   toNumber,
            status:      status,
            errorMsg:    errorMsg,
            payload:     null,            // no PII in WhatsApp payload log
            cancellationToken: cancellationToken);
    }

    /// <summary>Persists a <see cref="NotificationLog"/> audit record. Failures here are swallowed so they never surface to the caller.</summary>
    private async Task PersistNotificationLogAsync(
        Guid userId,
        NotificationType type,
        string templateKey,
        string recipient,
        NotificationStatus status,
        string? errorMsg,
        object? payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new NotificationLog
            {
                Id          = Guid.NewGuid(),
                UserId      = userId,
                Type        = type,
                TemplateKey = templateKey,
                Recipient   = recipient,
                Status      = status,
                ErrorMessage = errorMsg,
                Payload     = payload is not null ? JsonSerializer.Serialize(payload) : null,
                CreatedAt   = DateTimeOffset.UtcNow
            };

            await _notificationLogs.AddAsync(log);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist NotificationLog for {TemplateKey} to {Recipient}", templateKey, recipient);
        }
    }
}
