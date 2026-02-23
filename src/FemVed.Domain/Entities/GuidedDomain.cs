namespace FemVed.Domain.Entities;

/// <summary>
/// Top-level product domain, e.g. "Guided 1:1 Care".
/// A domain groups related categories.
/// </summary>
public class GuidedDomain
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Display name shown in UI navigation.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL slug, e.g. "guided-1-1-care". Must be unique.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Whether this domain is visible in the catalog.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    /// <summary>Categories belonging to this domain.</summary>
    public ICollection<GuidedCategory> Categories { get; set; } = new List<GuidedCategory>();
}
