using MediatR;

namespace FemVed.Application.Payments.Commands.ProcessCashfreeWebhook;

/// <summary>
/// Processes an inbound CashFree webhook event.
/// The signature is verified before any database mutations occur.
/// </summary>
/// <param name="RawPayload">Raw UTF-8 request body received from CashFree.</param>
/// <param name="Signature">Value of the <c>x-webhook-signature</c> header.</param>
/// <param name="Timestamp">Value of the <c>x-webhook-timestamp</c> header.</param>
public record ProcessCashfreeWebhookCommand(
    string RawPayload,
    string Signature,
    string Timestamp) : IRequest;
