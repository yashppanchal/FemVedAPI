using FemVed.Application.Interfaces;
using FemVed.Infrastructure.Notifications.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace FemVed.Infrastructure.Notifications;

/// <summary>
/// Sends WhatsApp messages via Twilio WhatsApp Business API (Meta pre-approved templates).
/// Implements <see cref="IWhatsAppService"/>.
/// </summary>
/// <remarks>
/// Two conditions must both be true for a message to be sent:
/// <list type="number">
///   <item><see cref="TwilioOptions.IsEnabled"/> = true  (global on/off via WHATSAPP_ENABLED env var)</item>
///   <item>The destination user's <c>WhatsAppOptIn</c> flag = true  (per-user preference, checked by the caller)</item>
/// </list>
/// This service only enforces the global switch; callers are responsible for the per-user check.
/// </remarks>
public sealed class TwilioWhatsAppService : IWhatsAppService
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioWhatsAppService> _logger;

    /// <summary>Initialises the service with typed Twilio options and initialises the Twilio client.</summary>
    public TwilioWhatsAppService(
        IOptions<TwilioOptions> options,
        ILogger<TwilioWhatsAppService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (_options.IsEnabled)
        {
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses Twilio Content Template API: parameters are passed as numbered variables
    /// in a body string that replicates the Meta-approved template format
    /// (e.g. "{{1}} {{2}} …"). For WhatsApp template messaging, message body must
    /// match the approved template exactly; <paramref name="templateParams"/> are
    /// interpolated in order.
    /// </remarks>
    public async Task SendAsync(
        string toNumber,
        string templateName,
        IEnumerable<string> templateParams,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogDebug("TwilioWhatsAppService: WhatsApp is globally disabled (WHATSAPP_ENABLED=false), skipping send");
            return;
        }

        if (string.IsNullOrWhiteSpace(toNumber))
        {
            _logger.LogWarning("TwilioWhatsAppService: toNumber is empty, skipping send for template '{TemplateName}'", templateName);
            return;
        }

        var toWhatsApp = toNumber.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
            ? toNumber
            : $"whatsapp:{toNumber}";

        // Build a simple numbered-variable body: "{{1}} {{2}} ..." so Twilio can match
        // the pre-approved template. The actual rendered content is controlled by Meta.
        var paramList = templateParams.ToList();
        var bodyParts = paramList.Select((p, i) => $"{{{{{i + 1}}}}}: {p}");
        var body = $"[{templateName}] " + string.Join(" | ", bodyParts);

        _logger.LogInformation(
            "TwilioWhatsAppService: sending '{TemplateName}' to {ToNumber}",
            templateName, toNumber);

        await MessageResource.CreateAsync(
            from: new PhoneNumber(_options.WhatsAppFrom),
            to:   new PhoneNumber(toWhatsApp),
            body: body);

        _logger.LogInformation(
            "TwilioWhatsAppService: '{TemplateName}' sent to {ToNumber}",
            templateName, toNumber);
    }
}
