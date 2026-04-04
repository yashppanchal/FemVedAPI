namespace FemVed.Domain.Entities;

/// <summary>
/// Pricing tier for library videos, e.g. "Movie", "Standard", "Medium", "Large".
/// Each tier has fixed prices per region defined in <see cref="LibraryTierPrice"/>.
/// </summary>
public class LibraryPriceTier
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Unique tier key, e.g. "MOVIE", "STANDARD".</summary>
    public string TierKey { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this tier is available for new videos.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    /// <summary>Region-specific prices for this tier.</summary>
    public ICollection<LibraryTierPrice> Prices { get; set; } = new List<LibraryTierPrice>();

    /// <summary>Videos assigned to this tier.</summary>
    public ICollection<LibraryVideo> Videos { get; set; } = new List<LibraryVideo>();
}
