using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FemVed.Application.Interfaces;
using FemVed.Infrastructure.Notifications.Options;
using FemVed.Infrastructure.Notifications.Templates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FemVed.Infrastructure.Notifications;

/// <summary>
/// Sends transactional emails via Resend (https://resend.com).
/// Templated sends (<see cref="SendAsync"/>) render an embedded HTML resource through
/// <see cref="ITemplateRenderer"/> and POST the rendered subject + HTML to Resend's
/// <c>/emails</c> endpoint. Raw sends (<see cref="SendRawAsync"/>) bypass the renderer
/// and POST the caller-supplied HTML directly.
///
/// All sends use the configured <c>From</c> address. Failures throw so callers can log
/// or recover; the contract matches the previous SendGrid implementation.
/// </summary>
public sealed class ResendEmailService : IEmailService
{
    private const string EmailsEndpoint = "https://api.resend.com/emails";

    private readonly HttpClient _http;
    private readonly ResendOptions _options;
    private readonly ITemplateRenderer _renderer;
    private readonly ILogger<ResendEmailService> _logger;

    /// <summary>Initialises the service with an HTTP client, typed options, the template renderer, and a logger.</summary>
    public ResendEmailService(
        HttpClient http,
        IOptions<ResendOptions> options,
        ITemplateRenderer renderer,
        ILogger<ResendEmailService> logger)
    {
        _http     = http;
        _options  = options.Value;
        _renderer = renderer;
        _logger   = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Looks up the template file at <c>Notifications/Templates/{templateKey}.html</c>.
    /// If no file exists, the email is skipped and a warning is logged — this matches
    /// the previous behaviour where a missing SendGrid template ID would silently no-op.
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
            _logger.LogWarning("ResendEmailService: toEmail is empty, skipping send for template {TemplateKey}", templateKey);
            return;
        }

        if (!_renderer.TemplateExists(templateKey))
        {
            _logger.LogWarning(
                "ResendEmailService: no template file found for key '{TemplateKey}', skipping send. " +
                "Add Notifications/Templates/{TemplateKey}.html to enable.",
                templateKey, templateKey);
            return;
        }

        _logger.LogInformation("ResendEmailService: sending '{TemplateKey}' to {ToEmail}", templateKey, toEmail);

        var rendered = _renderer.Render(templateKey, templateData);

        await PostAsync(
            toEmail:      toEmail,
            toName:       toName,
            subject:      rendered.Subject,
            html:         rendered.Html,
            replyToEmail: null,
            templateLabel: templateKey,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SendRawAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string? replyToEmail = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("ResendEmailService: toEmail is empty, skipping raw send for subject '{Subject}'", subject);
            return;
        }

        _logger.LogInformation("ResendEmailService: sending raw email '{Subject}' to {ToEmail}", subject, toEmail);

        await PostAsync(
            toEmail:      toEmail,
            toName:       toName,
            subject:      subject,
            html:         htmlBody,
            replyToEmail: replyToEmail,
            templateLabel: $"raw:{subject}",
            cancellationToken: cancellationToken);
    }

    private async Task PostAsync(
        string toEmail,
        string toName,
        string subject,
        string html,
        string? replyToEmail,
        string templateLabel,
        CancellationToken cancellationToken)
    {
        var fromHeader = string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromEmail
            : $"{_options.FromName} <{_options.FromEmail}>";

        var toHeader = string.IsNullOrWhiteSpace(toName)
            ? toEmail
            : $"{toName} <{toEmail}>";

        var payload = new ResendEmailRequest(
            From:    fromHeader,
            To:      new[] { toHeader },
            Subject: subject,
            Html:    html,
            ReplyTo: string.IsNullOrWhiteSpace(replyToEmail) ? null : replyToEmail);

        using var request = new HttpRequestMessage(HttpMethod.Post, EmailsEndpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _http.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "ResendEmailService: Resend returned {StatusCode} for '{TemplateLabel}' to {ToEmail}. Body: {Body}",
                response.StatusCode, templateLabel, toEmail, responseBody);

            throw new InvalidOperationException(
                $"Resend returned {(int)response.StatusCode} for '{templateLabel}'.");
        }

        _logger.LogInformation(
            "ResendEmailService: '{TemplateLabel}' accepted by Resend for {ToEmail}",
            templateLabel, toEmail);
    }

    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")]     string From,
        [property: JsonPropertyName("to")]       string[] To,
        [property: JsonPropertyName("subject")]  string Subject,
        [property: JsonPropertyName("html")]     string Html,
        [property: JsonPropertyName("reply_to")] string? ReplyTo);
}
