namespace FemVed.Application.Library.DTOs;

// ── Admin list / edit DTOs for the Wellness Library module ──────────────────

/// <summary>Admin list item for a library video (all statuses, including draft and archived).</summary>
public record AdminLibraryVideoListItem(
    Guid VideoId,
    string Title,
    string Slug,
    string? CardImage,
    string VideoType,
    string Status,
    string CategoryName,
    string ExpertName,
    string? TotalDuration,
    int? EpisodeCount,
    string PriceTierKey,
    int SortOrder,
    bool IsFeatured,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Full edit detail for a library video (admin use — includes all child records).</summary>
public record AdminLibraryVideoDetail(
    Guid VideoId,
    Guid CategoryId,
    Guid ExpertId,
    Guid PriceTierId,
    string Title,
    string Slug,
    string? Synopsis,
    string? Description,
    string? CardImage,
    string? HeroImage,
    string? IconEmoji,
    string? GradientClass,
    string? TrailerUrl,
    string? StreamUrl,
    string VideoType,
    string? TotalDuration,
    int? TotalDurationSeconds,
    string? ReleaseYear,
    bool IsFeatured,
    string? FeaturedLabel,
    int? FeaturedPosition,
    string Status,
    int SortOrder,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CategoryName,
    string ExpertName,
    string PriceTierKey,
    List<AdminEpisodeDto> Episodes,
    List<AdminVideoTagDto> Tags,
    List<AdminVideoFeatureDto> Features,
    List<AdminVideoTestimonialDto> Testimonials,
    List<AdminVideoPriceDto> PriceOverrides);

/// <summary>Episode detail for admin edit view.</summary>
public record AdminEpisodeDto(
    Guid EpisodeId,
    int EpisodeNumber,
    string Title,
    string? Description,
    string? Duration,
    int? DurationSeconds,
    string? StreamUrl,
    string? ThumbnailUrl,
    bool IsFreePreview,
    int SortOrder);

/// <summary>Tag detail for admin edit view.</summary>
public record AdminVideoTagDto(Guid TagId, string Tag, int SortOrder);

/// <summary>Feature detail for admin edit view.</summary>
public record AdminVideoFeatureDto(Guid FeatureId, string Icon, string Description, int SortOrder);

/// <summary>Testimonial detail for admin edit view.</summary>
public record AdminVideoTestimonialDto(
    Guid TestimonialId,
    string ReviewerName,
    string ReviewText,
    int Rating,
    int SortOrder,
    bool IsActive);

/// <summary>Price override detail for admin edit view.</summary>
public record AdminVideoPriceDto(
    Guid PriceId,
    string LocationCode,
    decimal Amount,
    string CurrencyCode,
    string CurrencySymbol,
    decimal? OriginalAmount);

/// <summary>Admin list item for a library domain.</summary>
public record AdminLibraryDomainDto(
    Guid DomainId,
    string Name,
    string Slug,
    int SortOrder,
    bool IsActive,
    int CategoryCount);

/// <summary>Admin list item for a library category.</summary>
public record AdminLibraryCategoryDto(
    Guid CategoryId,
    Guid DomainId,
    string Name,
    string Slug,
    int SortOrder,
    bool IsActive,
    int VideoCount);

/// <summary>Admin list item for a filter type.</summary>
public record AdminFilterTypeDto(
    Guid FilterTypeId,
    Guid DomainId,
    string Name,
    string FilterKey,
    string FilterTarget,
    int SortOrder,
    bool IsActive);

/// <summary>Admin list item for a price tier with its regional prices.</summary>
public record AdminPriceTierDto(
    Guid TierId,
    string TierKey,
    string DisplayName,
    int SortOrder,
    bool IsActive,
    List<AdminTierPriceDto> Prices);

/// <summary>Regional price within a tier.</summary>
public record AdminTierPriceDto(
    Guid PriceId,
    string LocationCode,
    decimal Amount,
    string CurrencyCode,
    string CurrencySymbol);
