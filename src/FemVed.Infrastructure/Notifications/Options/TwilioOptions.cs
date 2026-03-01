namespace FemVed.Infrastructure.Notifications.Options;

/// <summary>
/// Typed options for Twilio WhatsApp Business API.
/// Bound from environment variables via Options pattern.
/// </summary>
public sealed class TwilioOptions
{
    /// <summary>Twilio Account SID (ACxxxx).</summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>Twilio Auth Token.</summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Twilio WhatsApp sender number in the format <c>whatsapp:+14155238886</c>.
    /// </summary>
    public string WhatsAppFrom { get; set; } = string.Empty;

    /// <summary>
    /// Global on/off switch for WhatsApp delivery.
    /// Driven by the <c>WHATSAPP_ENABLED</c> environment variable.
    /// When <c>false</c>, <see cref="TwilioWhatsAppService"/> returns immediately without sending.
    /// Per-user opt-in (<c>user.WhatsAppOptIn</c>) is also required for a message to be sent.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Twilio SMS sender number in E.164 format, e.g. <c>+14155238886</c>.
    /// Used by <see cref="TwilioSmsService"/>.
    /// </summary>
    public string SmsFrom { get; set; } = string.Empty;

    /// <summary>
    /// Global on/off switch for SMS delivery.
    /// Driven by the <c>SMS_ENABLED</c> environment variable.
    /// When <c>false</c>, <see cref="TwilioSmsService"/> returns immediately without sending.
    /// </summary>
    public bool SmsEnabled { get; set; } = false;
}
