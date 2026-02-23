namespace FemVed.Application.Payments.DTOs;

/// <summary>
/// Represents a purchase order returned by <c>GET /api/v1/orders/{id}</c>
/// and <c>GET /api/v1/orders/my</c>.
/// </summary>
/// <param name="OrderId">Internal FemVed order UUID.</param>
/// <param name="UserId">The user who placed the order.</param>
/// <param name="DurationId">The program duration option purchased.</param>
/// <param name="AmountPaid">Final amount charged (after discount).</param>
/// <param name="CurrencyCode">ISO 4217 currency code.</param>
/// <param name="LocationCode">ISO country code at time of purchase.</param>
/// <param name="DiscountAmount">Coupon discount applied (0 if no coupon).</param>
/// <param name="Status">Order lifecycle status: Pending, Paid, Failed, Refunded.</param>
/// <param name="Gateway">Payment gateway used: CASHFREE or PAYPAL.</param>
/// <param name="GatewayOrderId">Gateway's order reference. May be null for failed/abandoned orders.</param>
/// <param name="FailureReason">Gateway error message for failed orders. Null otherwise.</param>
/// <param name="CreatedAt">UTC timestamp when the order was created.</param>
public record OrderDto(
    Guid OrderId,
    Guid UserId,
    Guid DurationId,
    decimal AmountPaid,
    string CurrencyCode,
    string LocationCode,
    decimal DiscountAmount,
    string Status,
    string Gateway,
    string? GatewayOrderId,
    string? FailureReason,
    DateTimeOffset CreatedAt);
