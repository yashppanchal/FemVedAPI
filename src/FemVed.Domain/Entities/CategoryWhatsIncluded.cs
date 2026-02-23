namespace FemVed.Domain.Entities;

/// <summary>A single bullet point in the "What's Included" list on a category hero page.</summary>
public class CategoryWhatsIncluded
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent category.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Bullet-point text shown on the category page.</summary>
    public string ItemText { get; set; } = string.Empty;

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    // Navigation
    /// <summary>The category this item belongs to.</summary>
    public GuidedCategory Category { get; set; } = null!;
}
