namespace FemVed.Infrastructure.Notifications.Options;

/// <summary>
/// Typed configuration for Resend (https://resend.com).
/// Bound from environment variables via the Options pattern in
/// <c>InfrastructureServiceExtensions</c>.
/// </summary>
public sealed class ResendOptions
{
    /// <summary>Resend API key, e.g. <c>re_xxxxxxxx</c>.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Verified sending email address (e.g. <c>hello@femved.com</c>).</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Display name shown next to <see cref="FromEmail"/> in recipients' inboxes.</summary>
    public string FromName { get; set; } = "FemVed";
}
