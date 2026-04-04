namespace FemVed.Domain.Entities;

/// <summary>
/// Content category within the Wellness Library, e.g. "Hormonal Health Support", "Mental &amp; Spiritual Wellbeing".
/// </summary>
public class LibraryCategory
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent library domain.</summary>
    public Guid DomainId { get; set; }

    /// <summary>Display name, e.g. "Hormonal Health Support".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL slug, e.g. "hormonal-health-support". Must be unique.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional description of this category.</summary>
    public string? Description { get; set; }

    /// <summary>Card image URL for this category.</summary>
    public string? CardImage { get; set; }

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this category is visible in the catalog.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    /// <summary>The parent domain.</summary>
    public LibraryDomain Domain { get; set; } = null!;

    /// <summary>Videos in this category.</summary>
    public ICollection<LibraryVideo> Videos { get; set; } = new List<LibraryVideo>();
}
