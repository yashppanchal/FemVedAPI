namespace FemVed.Application.Interfaces;

// ── Input / Output models shared by all payment gateways ─────────────────────

/// <summary>Input passed to a payment gateway when creating an external order.</summary>
/// <param name="InternalOrderId">Our internal Order UUID (used as the gateway's reference ID).</param>
/// <param name="Amount">Amount to charge.</param>
/// <param name="CurrencyCode">ISO 4217 currency code, e.g. "GBP".</param>
/// <param name="CustomerEmail">Customer's email address.</param>
/// <param name="CustomerName">Customer's full name.</param>
/// <param name="CustomerPhone">Customer's full mobile number, e.g. "+447890001234". May be null.</param>
public record CreateGatewayOrderRequest(
    string InternalOrderId,
    decimal Amount,
    string CurrencyCode,
    string CustomerEmail,
    string CustomerName,
    string? CustomerPhone);

/// <summary>Result returned by the gateway after a successful external order creation.</summary>
/// <param name="GatewayOrderId">Gateway's own order identifier (cf_xxx for CashFree, PayPal order id).</param>
/// <param name="PaymentSessionId">CashFree payment session token. Null for PayPal.</param>
/// <param name="ApprovalUrl">PayPal payer approval URL. Null for CashFree.</param>
public record GatewayCreateOrderResult(
    string GatewayOrderId,
    string? PaymentSessionId,
    string? ApprovalUrl);

/// <summary>Input passed to a payment gateway when requesting a refund.</summary>
/// <param name="GatewayOrderId">Gateway's order identifier.</param>
/// <param name="GatewayPaymentId">Gateway's payment/capture identifier. May be null for CashFree.</param>
/// <param name="InternalRefundId">Our internal Refund UUID — used as the gateway's idempotency key.</param>
/// <param name="Amount">Amount to refund.</param>
/// <param name="CurrencyCode">ISO 4217 currency code.</param>
/// <param name="Reason">Human-readable refund reason.</param>
public record GatewayRefundRequest(
    string GatewayOrderId,
    string? GatewayPaymentId,
    string InternalRefundId,
    decimal Amount,
    string CurrencyCode,
    string Reason);

/// <summary>Result returned by the gateway after a refund attempt.</summary>
/// <param name="Success">True if the refund was accepted by the gateway.</param>
/// <param name="GatewayRefundId">Gateway's refund identifier. Null on failure.</param>
/// <param name="FailureReason">Gateway error message when <see cref="Success"/> is false.</param>
public record GatewayRefundResult(
    bool Success,
    string? GatewayRefundId,
    string? FailureReason);

// ── Gateway interface ─────────────────────────────────────────────────────────

/// <summary>
/// Abstraction over a payment provider (CashFree or PayPal).
/// All implementations live in the Infrastructure layer.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Creates an external payment order and returns the gateway-specific tokens.</summary>
    /// <param name="request">Order details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Gateway order identifiers and tokens.</returns>
    Task<GatewayCreateOrderResult> CreateOrderAsync(
        CreateGatewayOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the authenticity of an inbound webhook payload.
    /// CashFree: validates HMAC-SHA256 locally.
    /// PayPal: calls PayPal's verify-webhook-signature API.
    /// </summary>
    /// <param name="rawPayload">Raw UTF-8 request body string.</param>
    /// <param name="headers">Relevant webhook headers keyed by lowercase header name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the signature is valid; otherwise false.</returns>
    Task<bool> VerifyWebhookSignatureAsync(
        string rawPayload,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a previously approved PayPal order, moving it to COMPLETED.
    /// Only applicable to PayPal — CashFree payments are captured automatically via the session.
    /// </summary>
    /// <param name="gatewayOrderId">The PayPal order ID returned by <see cref="CreateOrderAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The capture ID (gateway payment ID), or null on failure.</returns>
    Task<string?> CaptureOrderAsync(
        string gatewayOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>Initiates a refund against a previously captured payment.</summary>
    /// <param name="request">Refund details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refund result from the gateway.</returns>
    Task<GatewayRefundResult> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default);
}
