using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>
/// Discount coupon applicable at checkout.
/// Business rule: coupon discount is capped so the final price is never below 1 unit of the currency.
/// </summary>
public class Coupon
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Uppercase coupon code entered by the user, e.g. "WELCOME20". Must be unique.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Whether the discount is a percentage or a flat amount.</summary>
    public DiscountType DiscountType { get; set; }

    /// <summary>The discount value — percentage (0–100) or flat amount in the user's currency.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Minimum order amount (before discount) required to use this coupon. Null = no minimum.</summary>
    public decimal? MinOrderAmount { get; set; }

    /// <summary>Maximum number of times this coupon can be used. Null = unlimited.</summary>
    public int? MaxUses { get; set; }

    /// <summary>Current usage count.</summary>
    public int UsedCount { get; set; }

    /// <summary>UTC start of validity window. Null = valid immediately.</summary>
    public DateTimeOffset? ValidFrom { get; set; }

    /// <summary>UTC end of validity window. Null = never expires.</summary>
    public DateTimeOffset? ValidUntil { get; set; }

    /// <summary>Whether this coupon is currently redeemable.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
