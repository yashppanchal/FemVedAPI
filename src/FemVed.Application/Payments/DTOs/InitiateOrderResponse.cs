namespace FemVed.Application.Payments.DTOs;

/// <summary>
/// Returned by <c>POST /api/v1/orders/initiate</c>.
/// Fields are gateway-specific: PaymentSessionId is set for CashFree; ApprovalUrl is set for PayPal.
/// </summary>
/// <param name="OrderId">Internal FemVed order UUID.</param>
/// <param name="Gateway">Gateway name: "CASHFREE" or "PAYPAL".</param>
/// <param name="Amount">Final amount charged (after coupon discount).</param>
/// <param name="Currency">ISO 4217 currency code, e.g. "GBP", "INR".</param>
/// <param name="Symbol">Display symbol, e.g. "£", "₹".</param>
/// <param name="GatewayOrderId">Gateway's own order identifier.</param>
/// <param name="PaymentSessionId">CashFree payment session token. Null for PayPal.</param>
/// <param name="ApprovalUrl">PayPal payer approval URL. Null for CashFree.</param>
public record InitiateOrderResponse(
    Guid OrderId,
    string Gateway,
    decimal Amount,
    string Currency,
    string Symbol,
    string? GatewayOrderId,
    string? PaymentSessionId,
    string? ApprovalUrl);
