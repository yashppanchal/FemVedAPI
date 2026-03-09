namespace FemVed.Application.Admin.DTOs;

/// <summary>
/// Enriched order view returned by <c>GET /api/v1/admin/orders</c>.
/// Includes joined user name, program name, duration label, and coupon code.
/// Field names are intentionally matched to the frontend <c>AdminOrder</c> TypeScript interface.
/// </summary>
/// <param name="OrderId">Internal FemVed order UUID.</param>
/// <param name="UserId">The user who placed the order.</param>
/// <param name="UserName">Full name of the purchasing user.</param>
/// <param name="UserEmail">Email of the purchasing user.</param>
/// <param name="ProgramId">UUID of the purchased program.</param>
/// <param name="ProgramName">Name of the purchased program.</param>
/// <param name="DurationId">UUID of the purchased duration option.</param>
/// <param name="DurationLabel">Human-readable duration label, e.g. "6 weeks".</param>
/// <param name="Amount">Final amount charged (after discount).</param>
/// <param name="Currency">ISO 4217 currency code, e.g. "GBP".</param>
/// <param name="DiscountAmount">Coupon discount applied (0 if no coupon).</param>
/// <param name="CouponCode">Coupon code used, or null if none.</param>
/// <param name="Status">Order lifecycle status string.</param>
/// <param name="Gateway">Payment gateway used, e.g. "PayPal" or "CashFree".</param>
/// <param name="GatewayOrderId">Gateway's own order reference ID.</param>
/// <param name="CreatedAt">UTC timestamp when the order was created.</param>
public record AdminOrderDto(
    Guid OrderId,
    Guid UserId,
    string UserName,
    string UserEmail,
    Guid ProgramId,
    string ProgramName,
    Guid DurationId,
    string DurationLabel,
    decimal Amount,
    string Currency,
    decimal DiscountAmount,
    string? CouponCode,
    string Status,
    string Gateway,
    string? GatewayOrderId,
    DateTimeOffset CreatedAt);
