namespace FemVed.Domain.Entities;

/// <summary>
/// Location-specific price for a program duration.
/// A duration has at most one price per location code (IN / GB / US).
/// </summary>
public class DurationPrice
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent duration.</summary>
    public Guid DurationId { get; set; }

    /// <summary>ISO country code driving price selection, e.g. "IN", "GB", "US".</summary>
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>Price amount, e.g. 320.00.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency code, e.g. "GBP", "USD", "INR".</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Display currency symbol, e.g. "£", "$", "₹".</summary>
    public string CurrencySymbol { get; set; } = string.Empty;

    /// <summary>Whether this price is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    /// <summary>The duration this price belongs to.</summary>
    public ProgramDuration Duration { get; set; } = null!;
}
