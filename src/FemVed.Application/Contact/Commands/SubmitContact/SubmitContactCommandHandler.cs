using System.Net;
using FemVed.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Contact.Commands.SubmitContact;

/// <summary>
/// Handles <see cref="SubmitContactCommand"/>.
/// Sends a thank-you email to the submitter and a notification email to every address
/// listed in the <c>ADMIN_NOTIFICATION_EMAILS</c> configuration value.
/// </summary>
public sealed class SubmitContactCommandHandler : IRequestHandler<SubmitContactCommand>
{
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<SubmitContactCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public SubmitContactCommandHandler(
        IEmailService email,
        IConfiguration config,
        ILogger<SubmitContactCommandHandler> logger)
    {
        _email  = email;
        _config = config;
        _logger = logger;
    }

    /// <summary>Sends the thank-you and admin-notification emails.</summary>
    public async Task Handle(SubmitContactCommand request, CancellationToken cancellationToken)
    {
        var name    = request.Name.Trim();
        var email   = request.Email.Trim();
        var message = request.Message.Trim();

        _logger.LogInformation("Contact form submitted by {Email}", email);

        // 1. Thank-you email to the submitter.
        try
        {
            var submitterHtml = $@"
<p>Hi {WebUtility.HtmlEncode(name)},</p>
<p>Thank you for contacting FemVed. We've received your message and will get back to you as soon as we can.</p>
<p>For your reference, here's what you sent us:</p>
<blockquote style=""border-left:3px solid #56131b;padding:8px 12px;color:#444;background:#faf6f6;"">
{WebUtility.HtmlEncode(message).Replace("\n", "<br/>")}
</blockquote>
<p>Warmly,<br/>The FemVed Team</p>";

            await _email.SendRawAsync(
                toEmail:  email,
                toName:   name,
                subject:  "Thank you for contacting FemVed",
                htmlBody: submitterHtml,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Contact form: thank-you email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact form: failed to send thank-you email to {Email}", email);
        }

        // 2. Notification email(s) to admin(s).
        var adminEmails = (_config["ADMIN_NOTIFICATION_EMAILS"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (adminEmails.Length == 0)
        {
            _logger.LogWarning("Contact form: ADMIN_NOTIFICATION_EMAILS is empty — admin notification not sent");
            return;
        }

        var adminHtml = $@"
<p>You have a new message from the FemVed contact form:</p>
<table cellpadding=""4"" style=""border-collapse:collapse;"">
<tr><td><strong>Name</strong></td><td>{WebUtility.HtmlEncode(name)}</td></tr>
<tr><td><strong>Email</strong></td><td>{WebUtility.HtmlEncode(email)}</td></tr>
</table>
<p><strong>Message</strong></p>
<blockquote style=""border-left:3px solid #56131b;padding:8px 12px;color:#222;background:#faf6f6;"">
{WebUtility.HtmlEncode(message).Replace("\n", "<br/>")}
</blockquote>
<p style=""color:#666;font-size:12px;"">Reply directly to this email to respond — Reply-To is set to the submitter.</p>";

        foreach (var admin in adminEmails)
        {
            try
            {
                await _email.SendRawAsync(
                    toEmail:      admin,
                    toName:       "FemVed Admin",
                    subject:      $"New contact form message from {name}",
                    htmlBody:     adminHtml,
                    replyToEmail: email,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Contact form: admin notification sent to {AdminEmail}", admin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Contact form: failed to send admin notification to {AdminEmail}", admin);
            }
        }
    }
}
