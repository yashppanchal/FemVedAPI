namespace FemVed.Domain.Entities;

/// <summary>
/// Optional per-video price override for a specific location.
/// Falls back to the tier price if not set.
/// </summary>
public class LibraryVideoPrice
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the video.</summary>
    public Guid VideoId { get; set; }

    /// <summary>Location code, e.g. "IN", "GB", "US".</summary>
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>Override price amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency code, e.g. "INR", "GBP", "USD".</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Currency symbol for display, e.g. "₹", "£", "$".</summary>
    public string CurrencySymbol { get; set; } = string.Empty;

    /// <summary>Original amount for struck-through "was ₹X" display. Null if no strikethrough.</summary>
    public decimal? OriginalAmount { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    /// <summary>The video this price override belongs to.</summary>
    public LibraryVideo Video { get; set; } = null!;
}
