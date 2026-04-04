namespace FemVed.Domain.Entities;

/// <summary>
/// Region-specific price for a <see cref="LibraryPriceTier"/>.
/// One row per (tier, location_code) pair.
/// </summary>
public class LibraryTierPrice
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the price tier.</summary>
    public Guid TierId { get; set; }

    /// <summary>Location code, e.g. "IN", "GB", "US".</summary>
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>Price amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency code, e.g. "INR", "GBP", "USD".</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Currency symbol for display, e.g. "₹", "£", "$".</summary>
    public string CurrencySymbol { get; set; } = string.Empty;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    /// <summary>The parent price tier.</summary>
    public LibraryPriceTier Tier { get; set; } = null!;
}
