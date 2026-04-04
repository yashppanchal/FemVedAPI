namespace FemVed.Application.Library.DTOs;

// ── All record types below match the EXACT camelCase JSON contract in WELLNESS_LIBRARY_PROMPT.md §4.
// Field names must not be changed — the React frontend binds directly to this shape.

/// <summary>Root response for GET /api/v1/library/tree.</summary>
/// <param name="Domain">The library domain with hero images.</param>
/// <param name="Filters">Dynamic filter tabs (including synthetic "All Programs").</param>
/// <param name="FeaturedVideos">Admin-curated featured row (up to 3 cards).</param>
/// <param name="Categories">All active categories with their published videos.</param>
public record LibraryTreeResponse(
    LibraryDomainDto Domain,
    List<LibraryFilterDto> Filters,
    List<LibraryFeaturedVideoDto> FeaturedVideos,
    List<LibraryCategoryDto> Categories);

/// <summary>Library domain info for the tree response.</summary>
/// <param name="DomainId">Domain primary key.</param>
/// <param name="DomainName">Display name, e.g. "Wellness Library".</param>
/// <param name="HeroImageDesktop">Hero banner URL for desktop.</param>
/// <param name="HeroImageMobile">Hero banner URL for mobile.</param>
/// <param name="HeroImagePortrait">Hero banner URL portrait.</param>
public record LibraryDomainDto(
    Guid DomainId,
    string DomainName,
    string? HeroImageDesktop,
    string? HeroImageMobile,
    string? HeroImagePortrait);

/// <summary>A dynamic filter tab.</summary>
/// <param name="FilterId">Filter primary key.</param>
/// <param name="Name">Display label, e.g. "Masterclasses".</param>
/// <param name="FilterKey">Query key, e.g. "masterclass".</param>
/// <param name="FilterTarget">What the filter matches: "ALL", "VIDEO_TYPE", or "CATEGORY".</param>
public record LibraryFilterDto(
    Guid FilterId,
    string Name,
    string FilterKey,
    string FilterTarget);

/// <summary>A featured video card shown in the featured row.</summary>
/// <param name="VideoId">Video primary key.</param>
/// <param name="Position">Featured row position (1, 2, 3).</param>
/// <param name="EyebrowText">Featured label, e.g. "Editor's Choice . Series".</param>
/// <param name="Title">Video title.</param>
/// <param name="ExpertName">Expert display name.</param>
/// <param name="TotalDuration">Display duration, e.g. "4h 10m".</param>
/// <param name="EpisodeCount">Number of episodes (null for Masterclass).</param>
/// <param name="Price">Formatted price for the user's location.</param>
/// <param name="CardImage">Cover image URL.</param>
/// <param name="IconEmoji">Emoji icon for gradient fallback.</param>
/// <param name="GradientClass">CSS gradient class, e.g. "grad-1".</param>
/// <param name="VideoType">Content type: "MASTERCLASS" or "SERIES".</param>
public record LibraryFeaturedVideoDto(
    Guid VideoId,
    int? Position,
    string? EyebrowText,
    string Title,
    string ExpertName,
    string? TotalDuration,
    int? EpisodeCount,
    string Price,
    string? CardImage,
    string? IconEmoji,
    string? GradientClass,
    string VideoType);

/// <summary>A category with its published videos.</summary>
/// <param name="CategoryId">Category primary key.</param>
/// <param name="CategoryName">Display name.</param>
/// <param name="CategorySlug">URL slug.</param>
/// <param name="Videos">Published videos in this category.</param>
public record LibraryCategoryDto(
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    List<LibraryVideoCardDto> Videos);

/// <summary>A video card in the catalog grid.</summary>
/// <param name="VideoId">Video primary key.</param>
/// <param name="Title">Video title.</param>
/// <param name="Slug">URL slug.</param>
/// <param name="Synopsis">Short blurb for the card.</param>
/// <param name="CardImage">Cover image URL.</param>
/// <param name="IconEmoji">Emoji icon for gradient fallback.</param>
/// <param name="GradientClass">CSS gradient class.</param>
/// <param name="VideoType">Content type: "MASTERCLASS" or "SERIES".</param>
/// <param name="TotalDuration">Display duration, e.g. "4h 10m".</param>
/// <param name="EpisodeCount">Number of episodes (null for Masterclass).</param>
/// <param name="ReleaseYear">Release year, e.g. "2026".</param>
/// <param name="Price">Formatted price for the user's location.</param>
/// <param name="OriginalPrice">Struck-through original price, or null.</param>
/// <param name="ExpertName">Expert display name.</param>
/// <param name="ExpertTitle">Expert professional title.</param>
/// <param name="Tags">Display tags, e.g. ["Hormones", "Cycle"].</param>
public record LibraryVideoCardDto(
    Guid VideoId,
    string Title,
    string Slug,
    string? Synopsis,
    string? CardImage,
    string? IconEmoji,
    string? GradientClass,
    string VideoType,
    string? TotalDuration,
    int? EpisodeCount,
    string? ReleaseYear,
    string Price,
    string? OriginalPrice,
    string ExpertName,
    string ExpertTitle,
    List<string> Tags);
