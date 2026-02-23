using FemVed.Application.Payments.Commands.ProcessCashfreeWebhook;
using FemVed.Application.Payments.Commands.ProcessPaypalWebhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Receives inbound payment gateway webhooks.
/// Both endpoints are public (AllowAnonymous) — signature verification is
/// performed inside the command handler before any database mutations occur.
/// Base route: /api/v1/payments
/// </summary>
[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Receives a CashFree webhook event (PAYMENT_SUCCESS_WEBHOOK, PAYMENT_FAILED_WEBHOOK, etc.).
    /// Signature is verified via HMAC-SHA256 before any DB update.
    /// Returns 200 OK for all events (including unrecognised ones) so CashFree does not retry.
    /// Returns 401 Unauthorized if the signature is invalid.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success; 401 if signature invalid.</returns>
    [HttpPost("cashfree/webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CashfreeWebhook(CancellationToken cancellationToken)
    {
        var rawBody = await ReadRawBodyAsync(cancellationToken);

        var signature = Request.Headers["x-webhook-signature"].FirstOrDefault() ?? string.Empty;
        var timestamp = Request.Headers["x-webhook-timestamp"].FirstOrDefault() ?? string.Empty;

        await _mediator.Send(
            new ProcessCashfreeWebhookCommand(rawBody, signature, timestamp),
            cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Receives a PayPal webhook event (PAYMENT.CAPTURE.COMPLETED, etc.).
    /// Signature is verified by calling PayPal's verify-webhook-signature API before any DB update.
    /// Returns 200 OK for all events (including unrecognised ones) so PayPal does not retry.
    /// Returns 401 Unauthorized if the signature verification fails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success; 401 if signature invalid.</returns>
    [HttpPost("paypal/webhook")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PaypalWebhook(CancellationToken cancellationToken)
    {
        var rawBody = await ReadRawBodyAsync(cancellationToken);

        var authAlgo       = Request.Headers["paypal-auth-algo"].FirstOrDefault() ?? string.Empty;
        var certUrl        = Request.Headers["paypal-cert-url"].FirstOrDefault() ?? string.Empty;
        var transmissionId = Request.Headers["paypal-transmission-id"].FirstOrDefault() ?? string.Empty;
        var transmissionSig= Request.Headers["paypal-transmission-sig"].FirstOrDefault() ?? string.Empty;
        var transmissionTime= Request.Headers["paypal-transmission-time"].FirstOrDefault() ?? string.Empty;

        await _mediator.Send(
            new ProcessPaypalWebhookCommand(
                rawBody, authAlgo, certUrl,
                transmissionId, transmissionSig, transmissionTime),
            cancellationToken);

        return Ok();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Reads the raw request body as a UTF-8 string without consuming the stream irreversibly.</summary>
    private async Task<string> ReadRawBodyAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;
        return body;
    }
}
