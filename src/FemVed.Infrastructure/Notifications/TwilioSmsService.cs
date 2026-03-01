using FemVed.Application.Interfaces;
using FemVed.Infrastructure.Notifications.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace FemVed.Infrastructure.Notifications;

/// <summary>
/// Sends plain-text SMS messages via Twilio SMS API.
/// Implements <see cref="ISmsService"/>.
/// </summary>
/// <remarks>
/// The global switch <see cref="TwilioOptions.SmsEnabled"/> must be <c>true</c> for any
/// message to be dispatched. When <c>false</c>, the method returns immediately without sending.
/// </remarks>
public sealed class TwilioSmsService : ISmsService
{
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioSmsService> _logger;

    /// <summary>Initialises the service with typed Twilio options and initialises the Twilio client.</summary>
    public TwilioSmsService(
        IOptions<TwilioOptions> options,
        ILogger<TwilioSmsService> logger)
    {
        _options = options.Value;
        _logger  = logger;

        if (_options.SmsEnabled)
        {
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="toNumber"/> must be in E.164 format, e.g. <c>+917890001234</c>.
    /// </remarks>
    public async Task SendAsync(
        string toNumber,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!_options.SmsEnabled)
        {
            _logger.LogDebug("TwilioSmsService: SMS is globally disabled (SMS_ENABLED=false), skipping send");
            return;
        }

        if (string.IsNullOrWhiteSpace(toNumber))
        {
            _logger.LogWarning("TwilioSmsService: toNumber is empty, skipping send");
            return;
        }

        _logger.LogInformation("TwilioSmsService: sending SMS to {ToNumber}", toNumber);

        await MessageResource.CreateAsync(
            from: new PhoneNumber(_options.SmsFrom),
            to:   new PhoneNumber(toNumber),
            body: body);

        _logger.LogInformation("TwilioSmsService: SMS sent to {ToNumber}", toNumber);
    }
}
