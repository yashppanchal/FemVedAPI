using FemVed.Application.Interfaces;
using FemVed.Infrastructure.Notifications.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace FemVed.Infrastructure.Notifications;

/// <summary>
/// Sends transactional emails via SendGrid dynamic templates.
/// Implements <see cref="IEmailService"/>.
/// </summary>
public sealed class SendGridEmailService : IEmailService
{
    private readonly SendGridOptions _options;
    private readonly ILogger<SendGridEmailService> _logger;

    /// <summary>Initialises the service with typed SendGrid options.</summary>
    public SendGridEmailService(
        IOptions<SendGridOptions> options,
        ILogger<SendGridEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Looks up the SendGrid dynamic template ID from <see cref="SendGridOptions.TemplateIds"/>
    /// using <paramref name="templateKey"/> as the dictionary key.
    /// If the key is not found, the email is skipped and a warning is logged.
    /// Template data values are serialised via <c>ToString()</c> — callers should ensure
    /// values are primitives or override <c>ToString()</c> appropriately.
    /// </remarks>
    public async Task SendAsync(
        string toEmail,
        string toName,
        string templateKey,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("SendGridEmailService: toEmail is empty, skipping send for template {TemplateKey}", templateKey);
            return;
        }

        if (!_options.TemplateIds.TryGetValue(templateKey, out var templateId))
        {
            _logger.LogWarning("SendGridEmailService: no template ID configured for key '{TemplateKey}', skipping send", templateKey);
            return;
        }

        _logger.LogInformation("SendGridEmailService: sending '{TemplateKey}' to {ToEmail}", templateKey, toEmail);

        var client  = new SendGridClient(_options.ApiKey);
        var from    = new EmailAddress(_options.FromEmail, _options.FromName);
        var to      = new EmailAddress(toEmail, toName);
        var message = MailHelper.CreateSingleTemplateEmail(from, to, templateId, templateData);

        var response = await client.SendEmailAsync(message, cancellationToken);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "SendGridEmailService: SendGrid returned {StatusCode} for template '{TemplateKey}' to {ToEmail}. Body: {Body}",
                response.StatusCode, templateKey, toEmail, body);

            throw new InvalidOperationException(
                $"SendGrid returned {response.StatusCode} for template '{templateKey}'.");
        }

        _logger.LogInformation("SendGridEmailService: '{TemplateKey}' delivered to {ToEmail}", templateKey, toEmail);
    }
}
