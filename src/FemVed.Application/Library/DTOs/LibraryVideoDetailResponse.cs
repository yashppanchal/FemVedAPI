namespace FemVed.Application.Library.DTOs;

// ── Matches the Video Detail API JSON contract in WELLNESS_LIBRARY_PROMPT.md §4.
// Field names must not be changed — the React frontend binds directly to this shape.

/// <summary>Full detail response for GET /api/v1/library/videos/{slug}.</summary>
/// <param name="VideoId">Video primary key.</param>
/// <param name="Title">Full video title.</param>
/// <param name="Slug">URL slug.</param>
/// <param name="Synopsis">Short blurb.</param>
/// <param name="Description">Long rich text (HTML).</param>
/// <param name="CardImage">Grid card cover image URL.</param>
/// <param name="HeroImage">Detail page hero image URL.</param>
/// <param name="IconEmoji">Emoji icon for gradient fallback.</param>
/// <param name="GradientClass">CSS gradient class.</param>
/// <param name="TrailerUrl">YouTube embed URL for the public trailer.</param>
/// <param name="VideoType">Content type: "MASTERCLASS" or "SERIES".</param>
/// <param name="TotalDuration">Display duration, e.g. "4h 10m".</param>
/// <param name="ReleaseYear">Release year, e.g. "2026".</param>
/// <param name="Price">Formatted price for the user's location.</param>
/// <param name="OriginalPrice">Struck-through original price, or null.</param>
/// <param name="PriceTier">Tier key, e.g. "LARGE".</param>
/// <param name="ExpertId">Expert primary key.</param>
/// <param name="ExpertName">Expert display name.</param>
/// <param name="ExpertTitle">Expert professional title.</param>
/// <param name="ExpertGridDescription">Short expert bio.</param>
/// <param name="Tags">Display tags.</param>
/// <param name="Episodes">Episode list (locked, no stream URLs).</param>
/// <param name="Features">"What's included" features on the purchase card.</param>
/// <param name="Testimonials">Admin-curated testimonials.</param>
/// <param name="IsPurchased">Whether the authenticated user has purchased this video.</param>
public record LibraryVideoDetailResponse(
    Guid VideoId,
    string Title,
    string Slug,
    string? Synopsis,
    string? Description,
    string? CardImage,
    string? HeroImage,
    string? IconEmoji,
    string? GradientClass,
    string? TrailerUrl,
    string VideoType,
    string? TotalDuration,
    string? ReleaseYear,
    string Price,
    string? OriginalPrice,
    string PriceTier,
    Guid ExpertId,
    string ExpertName,
    string ExpertTitle,
    string? ExpertGridDescription,
    List<string> Tags,
    List<LibraryEpisodeDto> Episodes,
    List<LibraryFeatureDto> Features,
    List<LibraryTestimonialDto> Testimonials,
    bool IsPurchased);

/// <summary>An episode in the public detail view (locked — no stream URL).</summary>
/// <param name="EpisodeId">Episode primary key.</param>
/// <param name="EpisodeNumber">1-based episode number.</param>
/// <param name="Title">Episode title.</param>
/// <param name="Description">Episode description.</param>
/// <param name="Duration">Display duration, e.g. "18 min".</param>
/// <param name="IsFreePreview">Whether this episode is viewable without purchase.</param>
/// <param name="IsLocked">True unless the user has purchased or it is a free preview.</param>
public record LibraryEpisodeDto(
    Guid EpisodeId,
    int EpisodeNumber,
    string Title,
    string? Description,
    string? Duration,
    bool IsFreePreview,
    bool IsLocked);

/// <summary>A "What's included" feature on the purchase card.</summary>
/// <param name="Icon">Icon character, e.g. "▶", "♾".</param>
/// <param name="Description">Feature description.</param>
public record LibraryFeatureDto(string Icon, string Description);

/// <summary>An admin-curated testimonial on a library video.</summary>
/// <param name="ReviewerName">Reviewer name.</param>
/// <param name="ReviewText">Review text.</param>
/// <param name="Rating">Star rating (1–5).</param>
public record LibraryTestimonialDto(string ReviewerName, string ReviewText, int Rating);
