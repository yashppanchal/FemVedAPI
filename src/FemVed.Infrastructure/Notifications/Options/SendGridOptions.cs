namespace FemVed.Infrastructure.Notifications.Options;

/// <summary>
/// Typed options for SendGrid dynamic-template email delivery.
/// Bound from environment variables via Options pattern.
/// </summary>
public sealed class SendGridOptions
{
    /// <summary>SendGrid API key (SG.xxxxx).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>From address, e.g. hello@femved.com.</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>From display name, e.g. FemVed.</summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Maps internal template keys (e.g. "purchase_success") to SendGrid dynamic template IDs (e.g. "d-xxx").
    /// Populated from <c>SENDGRID_TEMPLATE_*</c> environment variables at startup.
    /// </summary>
    public Dictionary<string, string> TemplateIds { get; set; } = new();
}
