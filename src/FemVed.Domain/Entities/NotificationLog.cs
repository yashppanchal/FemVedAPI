using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>Audit record of every notification attempt (email, WhatsApp, SMS). Never stores PII in payload.</summary>
public class NotificationLog
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the recipient user. Null for system-generated notifications not linked to a user.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Delivery channel used.</summary>
    public NotificationType Type { get; set; }

    /// <summary>SendGrid template key or Twilio template name, e.g. "purchase_success".</summary>
    public string TemplateKey { get; set; } = string.Empty;

    /// <summary>Email address or phone number that received the notification.</summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>Delivery outcome.</summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Sent;

    /// <summary>Error detail if status is FAILED.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Template variable payload as JSON. Must not contain PII (passwords, card data).</summary>
    public string? Payload { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    /// <summary>The user this notification was sent to (may be null).</summary>
    public User? User { get; set; }
}
