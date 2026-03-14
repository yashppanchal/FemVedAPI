namespace FemVed.Application.Payments.DTOs;

/// <summary>
/// Represents a purchase order returned by <c>GET /api/v1/orders/my</c>.
/// Field names match the camelCase contract expected by the React frontend.
/// </summary>
/// <param name="OrderId">Internal FemVed order UUID.</param>
/// <param name="UserId">The user who placed the order.</param>
/// <param name="ProgramId">UUID of the program purchased.</param>
/// <param name="ProgramName">Display name of the program (null if program was deleted).</param>
/// <param name="DurationId">The program duration option purchased.</param>
/// <param name="DurationLabel">Human-readable duration, e.g. "6 weeks".</param>
/// <param name="Amount">Final amount charged (after discount).</param>
/// <param name="Currency">ISO 4217 currency code, e.g. "GBP".</param>
/// <param name="LocationCode">ISO country code at time of purchase.</param>
/// <param name="CouponCode">Coupon code applied (null if no coupon).</param>
/// <param name="DiscountAmount">Coupon discount applied (0 if no coupon).</param>
/// <param name="Status">Order lifecycle status: Pending, Paid, Failed, Refunded.</param>
/// <param name="Gateway">Payment gateway used: CASHFREE or PAYPAL.</param>
/// <param name="GatewayOrderId">Gateway's order reference. May be null for failed/abandoned orders.</param>
/// <param name="FailureReason">Gateway error message for failed orders. Null otherwise.</param>
/// <param name="CreatedAt">UTC timestamp when the order was created.</param>
public record OrderDto(
    Guid OrderId,
    Guid UserId,
    Guid? ProgramId,
    string? ProgramName,
    Guid DurationId,
    string DurationLabel,
    decimal Amount,
    string Currency,
    string LocationCode,
    string? CouponCode,
    decimal DiscountAmount,
    string Status,
    string Gateway,
    string? GatewayOrderId,
    string? FailureReason,
    DateTimeOffset CreatedAt);
