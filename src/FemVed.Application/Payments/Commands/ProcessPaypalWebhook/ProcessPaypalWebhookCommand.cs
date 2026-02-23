using MediatR;

namespace FemVed.Application.Payments.Commands.ProcessPaypalWebhook;

/// <summary>
/// Processes an inbound PayPal webhook event.
/// All PayPal verification headers must be forwarded from the original HTTP request.
/// Signature verification is performed via PayPal's verify-webhook-signature API.
/// </summary>
/// <param name="RawPayload">Raw UTF-8 request body received from PayPal.</param>
/// <param name="AuthAlgo">Value of the <c>paypal-auth-algo</c> header.</param>
/// <param name="CertUrl">Value of the <c>paypal-cert-url</c> header.</param>
/// <param name="TransmissionId">Value of the <c>paypal-transmission-id</c> header.</param>
/// <param name="TransmissionSig">Value of the <c>paypal-transmission-sig</c> header.</param>
/// <param name="TransmissionTime">Value of the <c>paypal-transmission-time</c> header.</param>
public record ProcessPaypalWebhookCommand(
    string RawPayload,
    string AuthAlgo,
    string CertUrl,
    string TransmissionId,
    string TransmissionSig,
    string TransmissionTime) : IRequest;
