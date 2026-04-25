namespace FemVed.Application.Interfaces;

/// <summary>Sends transactional emails via SendGrid dynamic templates or raw HTML bodies.</summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a templated email to a single recipient.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="toName">Recipient display name.</param>
    /// <param name="templateKey">SendGrid dynamic template key, e.g. "purchase_success".</param>
    /// <param name="templateData">Template variable dictionary (must not contain passwords or card data).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(string toEmail, string toName, string templateKey, Dictionary<string, object> templateData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a one-off email with a raw HTML body — used for ad-hoc notifications that do
    /// not warrant a SendGrid dynamic template (e.g. contact-form replies, feedback links).
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="toName">Recipient display name.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="htmlBody">HTML message body.</param>
    /// <param name="replyToEmail">Optional Reply-To address (e.g. the original sender on a contact form).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendRawAsync(string toEmail, string toName, string subject, string htmlBody, string? replyToEmail = null, CancellationToken cancellationToken = default);
}
