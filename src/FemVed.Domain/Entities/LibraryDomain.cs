namespace FemVed.Domain.Entities;

/// <summary>
/// Top-level domain container for the Wellness Library, e.g. "Wellness Library".
/// A domain groups related library categories.
/// </summary>
public class LibraryDomain
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Display name shown in UI, e.g. "Wellness Library".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL slug, e.g. "wellness-library". Must be unique.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional description of this domain.</summary>
    public string? Description { get; set; }

    /// <summary>Hero banner image URL for desktop.</summary>
    public string? HeroImageDesktop { get; set; }

    /// <summary>Hero banner image URL for mobile.</summary>
    public string? HeroImageMobile { get; set; }

    /// <summary>Hero banner image URL portrait.</summary>
    public string? HeroImagePortrait { get; set; }

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this domain is visible in the catalog.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    /// <summary>Categories belonging to this domain.</summary>
    public ICollection<LibraryCategory> Categories { get; set; } = new List<LibraryCategory>();

    /// <summary>Dynamic filter tabs for this domain.</summary>
    public ICollection<LibraryFilterType> FilterTypes { get; set; } = new List<LibraryFilterType>();
}
