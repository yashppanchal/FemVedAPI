namespace FemVed.Application.Interfaces;

/// <summary>Sends SMS messages via Twilio SMS API.</summary>
public interface ISmsService
{
    /// <summary>
    /// Sends a plain-text SMS message.
    /// </summary>
    /// <param name="toNumber">International phone number including dial code, e.g. "+917890001234".</param>
    /// <param name="body">Message body text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(string toNumber, string body, CancellationToken cancellationToken = default);
}
