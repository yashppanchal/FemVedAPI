namespace FemVed.Application.Interfaces;

/// <summary>Sends transactional emails via SendGrid dynamic templates.</summary>
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
}
