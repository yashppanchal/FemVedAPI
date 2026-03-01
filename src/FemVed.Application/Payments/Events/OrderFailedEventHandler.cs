using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Events;

/// <summary>
/// Handles <see cref="OrderFailedEvent"/>.
/// Sends the <c>purchase_failed</c> email to the user and logs the attempt to <see cref="NotificationLog"/>.
/// Notification failures are swallowed — they never roll back payment state.
/// </summary>
public sealed class OrderFailedEventHandler : INotificationHandler<OrderFailedEvent>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<NotificationLog> _notificationLogs;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<OrderFailedEventHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public OrderFailedEventHandler(
        IRepository<User> users,
        IRepository<NotificationLog> notificationLogs,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<OrderFailedEventHandler> logger)
    {
        _users            = users;
        _notificationLogs = notificationLogs;
        _emailService     = emailService;
        _uow              = uow;
        _logger           = logger;
    }

    /// <summary>Sends the purchase-failed email and logs the notification attempt.</summary>
    /// <param name="notification">The domain event payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(OrderFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "OrderFailedEvent received: Order={OrderId}, User={UserId}",
            notification.OrderId, notification.UserId);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == notification.UserId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning(
                "OrderFailedEventHandler: user {UserId} not found, skipping purchase_failed email",
                notification.UserId);
            return;
        }

        var templateData = new Dictionary<string, object>
        {
            ["first_name"] = user.FirstName,
            ["order_id"]   = notification.OrderId.ToString()
        };

        var status       = NotificationStatus.Sent;
        string? errorMsg = null;

        try
        {
            await _emailService.SendAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "purchase_failed",
                templateData,
                cancellationToken);

            _logger.LogInformation(
                "OrderFailedEventHandler: purchase_failed email sent to {Email} for order {OrderId}",
                user.Email, notification.OrderId);
        }
        catch (Exception ex)
        {
            status   = NotificationStatus.Failed;
            errorMsg = ex.Message;
            _logger.LogError(ex,
                "OrderFailedEventHandler: failed to send purchase_failed email to {Email} for order {OrderId}",
                user.Email, notification.OrderId);
        }

        try
        {
            await _notificationLogs.AddAsync(new NotificationLog
            {
                Id           = Guid.NewGuid(),
                UserId       = user.Id,
                Type         = NotificationType.Email,
                TemplateKey  = "purchase_failed",
                Recipient    = user.Email,
                Status       = status,
                ErrorMessage = errorMsg,
                Payload      = JsonSerializer.Serialize(templateData),
                CreatedAt    = DateTimeOffset.UtcNow
            });
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "OrderFailedEventHandler: failed to persist NotificationLog for order {OrderId}",
                notification.OrderId);
        }
    }
}
