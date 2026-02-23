namespace FemVed.Application.Interfaces;

/// <summary>Sends WhatsApp messages via Twilio WhatsApp Business API (Meta pre-approved templates).</summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Sends a pre-approved WhatsApp template message.
    /// </summary>
    /// <param name="toNumber">International phone number including dial code, e.g. "+447890001234".</param>
    /// <param name="templateName">Twilio/Meta approved template name, e.g. "purchase_confirmation_wa".</param>
    /// <param name="templateParams">Ordered list of template variable values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(string toNumber, string templateName, IEnumerable<string> templateParams, CancellationToken cancellationToken = default);
}
