namespace FemVed.Application.Admin.DTOs;

/// <summary>Coupon details for admin management.</summary>
/// <param name="CouponId">Coupon UUID.</param>
/// <param name="Code">Uppercase coupon code, e.g. "WELCOME20".</param>
/// <param name="DiscountType">Discount calculation method: "Percentage" or "Flat".</param>
/// <param name="DiscountValue">Discount amount — percentage (0–100) or flat currency value.</param>
/// <param name="MinOrderAmount">Minimum order amount required to apply this coupon. Null = no minimum.</param>
/// <param name="MaxUses">Maximum redemptions allowed. Null = unlimited.</param>
/// <param name="UsedCount">Current redemption count.</param>
/// <param name="ValidFrom">UTC start of validity window. Null = valid immediately.</param>
/// <param name="ValidUntil">UTC end of validity window. Null = never expires.</param>
/// <param name="IsActive">Whether this coupon is currently redeemable.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC last-update timestamp.</param>
public record CouponDto(
    Guid CouponId,
    string Code,
    string DiscountType,
    decimal DiscountValue,
    decimal? MinOrderAmount,
    int? MaxUses,
    int UsedCount,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
