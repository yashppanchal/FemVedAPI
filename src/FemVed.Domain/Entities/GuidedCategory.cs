namespace FemVed.Domain.Entities;

/// <summary>
/// A category page within a guided domain (e.g. "Hormonal Health Support").
/// Supports subcategories via self-referencing <see cref="ParentId"/>.
/// </summary>
public class GuidedCategory
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent <see cref="GuidedDomain"/>.</summary>
    public Guid DomainId { get; set; }

    /// <summary>FK to parent category for nested categories. Null = top-level.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Full display name, e.g. "Hormonal Health Support".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL slug, e.g. "hormonal-health-support". Must be unique.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Short label type used in card/grid display, e.g. "Hormonal Health Support".</summary>
    public string CategoryType { get; set; } = string.Empty;

    /// <summary>Hero section headline, e.g. "Get Guided Hormonal Care".</summary>
    public string HeroTitle { get; set; } = string.Empty;

    /// <summary>Hero section supporting copy.</summary>
    public string HeroSubtext { get; set; } = string.Empty;

    /// <summary>Call-to-action button label, e.g. "Book Your Program".</summary>
    public string? CtaLabel { get; set; }

    /// <summary>Call-to-action link, e.g. "/guided/hormonal-health-support".</summary>
    public string? CtaLink { get; set; }

    /// <summary>Section header above the program grid on the category page.</summary>
    public string? PageHeader { get; set; }

    /// <summary>Hero or card image URL (hosted on Cloudflare R2).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Display ordering within the domain (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether the category is visible in the catalog.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Soft-delete flag. Never hard-delete categories.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The domain this category belongs to.</summary>
    public GuidedDomain Domain { get; set; } = null!;

    /// <summary>Parent category (null for top-level).</summary>
    public GuidedCategory? Parent { get; set; }

    /// <summary>Child subcategories.</summary>
    public ICollection<GuidedCategory> Children { get; set; } = new List<GuidedCategory>();

    /// <summary>Bullet points for the "What's Included" section on the category hero.</summary>
    public ICollection<CategoryWhatsIncluded> WhatsIncluded { get; set; } = new List<CategoryWhatsIncluded>();

    /// <summary>Key support areas listed on the category page.</summary>
    public ICollection<CategoryKeyArea> KeyAreas { get; set; } = new List<CategoryKeyArea>();

    /// <summary>Programs listed under this category.</summary>
    public ICollection<Program> Programs { get; set; } = new List<Program>();
}
