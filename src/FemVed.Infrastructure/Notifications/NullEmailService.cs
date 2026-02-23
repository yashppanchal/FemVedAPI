using FemVed.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FemVed.Infrastructure.Notifications;

/// <summary>
/// No-op implementation of <see cref="IEmailService"/> used until Phase 5 (SendGrid).
/// Logs a warning for every send attempt so developers know emails are not being dispatched.
/// Replace with <c>SendGridEmailService</c> in Phase 5.
/// </summary>
public sealed class NullEmailService : IEmailService
{
    private readonly ILogger<NullEmailService> _logger;

    /// <summary>Initialises the null email service with a logger.</summary>
    /// <param name="logger">Logger instance.</param>
    public NullEmailService(ILogger<NullEmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simulates sending an email by logging the attempt. No email is actually dispatched.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="toName">Recipient display name.</param>
    /// <param name="templateKey">SendGrid template key (logged for diagnostics).</param>
    /// <param name="templateData">Template variables (not logged — may contain sensitive links).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task SendAsync(
        string toEmail,
        string toName,
        string templateKey,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "NullEmailService: email NOT sent — template={TemplateKey} to={ToEmail}. Wire up SendGrid in Phase 5.",
            templateKey, toEmail);

        return Task.CompletedTask;
    }
}
