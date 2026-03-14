using MediatR;

namespace FemVed.Application.Payments.Commands.ProcessStripeWebhook;

/// <summary>
/// Command that carries the raw Stripe webhook payload and its signature header
/// for verification and processing.
/// </summary>
/// <param name="RawPayload">Raw UTF-8 request body from Stripe.</param>
/// <param name="StripeSignature">Value of the <c>Stripe-Signature</c> header.</param>
public record ProcessStripeWebhookCommand(
    string RawPayload,
    string StripeSignature) : IRequest;
