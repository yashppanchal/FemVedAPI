using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>
/// Core content unit in the Wellness Library — a purchasable video or series.
/// Status flow: DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED.
/// Soft-deletable.
/// </summary>
public class LibraryVideo
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the category this video belongs to.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>FK to the expert who created this content.</summary>
    public Guid ExpertId { get; set; }

    /// <summary>FK to the pricing tier for this video.</summary>
    public Guid PriceTierId { get; set; }

    /// <summary>Full video title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>URL slug, e.g. "cycle-reset-method". Must be unique.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Short blurb for the grid card.</summary>
    public string? Synopsis { get; set; }

    /// <summary>Long rich text for the detail page (supports HTML).</summary>
    public string? Description { get; set; }

    /// <summary>Grid card cover image URL.</summary>
    public string? CardImage { get; set; }

    /// <summary>Detail page hero image URL.</summary>
    public string? HeroImage { get; set; }

    /// <summary>Emoji icon for gradient card fallback, e.g. "🧘".</summary>
    public string? IconEmoji { get; set; }

    /// <summary>CSS gradient class for card fallback, e.g. "grad-1".</summary>
    public string? GradientClass { get; set; }

    /// <summary>YouTube embed URL for the public trailer (viewable without purchase).</summary>
    public string? TrailerUrl { get; set; }

    /// <summary>YouTube embed URL for the full video (gated behind purchase). For Masterclass type only.</summary>
    public string? StreamUrl { get; set; }

    /// <summary>Content type: MASTERCLASS (single video) or SERIES (multiple episodes).</summary>
    public VideoType VideoType { get; set; }

    /// <summary>Display string for total duration, e.g. "4h 10m".</summary>
    public string? TotalDuration { get; set; }

    /// <summary>Total duration in seconds for sorting/filtering.</summary>
    public int? TotalDurationSeconds { get; set; }

    /// <summary>Release year for display, e.g. "2026".</summary>
    public string? ReleaseYear { get; set; }

    /// <summary>Whether this video appears in the featured row.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Eyebrow text shown on the featured card, e.g. "Editor's Choice · Series".</summary>
    public string? FeaturedLabel { get; set; }

    /// <summary>Position in the featured row (1, 2, 3).</summary>
    public int? FeaturedPosition { get; set; }

    /// <summary>Current lifecycle status.</summary>
    public VideoStatus Status { get; set; } = VideoStatus.Draft;

    /// <summary>Display ordering within the category (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>Soft-delete flag. Never hard-delete videos.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The category this video belongs to.</summary>
    public LibraryCategory Category { get; set; } = null!;

    /// <summary>The expert who created this content.</summary>
    public Expert Expert { get; set; } = null!;

    /// <summary>The pricing tier for this video.</summary>
    public LibraryPriceTier PriceTier { get; set; } = null!;

    /// <summary>Episodes (for Series type only).</summary>
    public ICollection<LibraryVideoEpisode> Episodes { get; set; } = new List<LibraryVideoEpisode>();

    /// <summary>Tags for search and display.</summary>
    public ICollection<LibraryVideoTag> Tags { get; set; } = new List<LibraryVideoTag>();

    /// <summary>"What's included" features shown on the purchase card.</summary>
    public ICollection<LibraryVideoFeature> Features { get; set; } = new List<LibraryVideoFeature>();

    /// <summary>Admin-curated testimonials/reviews.</summary>
    public ICollection<LibraryVideoTestimonial> Testimonials { get; set; } = new List<LibraryVideoTestimonial>();

    /// <summary>Per-video price overrides by location.</summary>
    public ICollection<LibraryVideoPrice> PriceOverrides { get; set; } = new List<LibraryVideoPrice>();

    /// <summary>Purchase access records.</summary>
    public ICollection<UserLibraryAccess> UserAccesses { get; set; } = new List<UserLibraryAccess>();
}
