namespace FemVed.Domain.Entities;

/// <summary>A key support area item listed on a category page, e.g. "PCOS, endometriosis, and cycle health".</summary>
public class CategoryKeyArea
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent category.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Area description displayed on the category page.</summary>
    public string AreaText { get; set; } = string.Empty;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    // Navigation
    /// <summary>The category this key area belongs to.</summary>
    public GuidedCategory Category { get; set; } = null!;
}
