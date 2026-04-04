using MediatR;

namespace FemVed.Application.Library.Commands.CreateLibraryVideo;

/// <summary>
/// Input record for a video feature (icon + description).
/// </summary>
/// <param name="Icon">Icon character, e.g. "▶", "♾".</param>
/// <param name="Description">Feature description text.</param>
public record CreateVideoFeatureInput(string Icon, string Description);

/// <summary>
/// Creates a new library video in Draft status with optional tags and features.
/// Returns the newly created video's ID.
/// </summary>
/// <param name="CategoryId">FK to the category this video belongs to.</param>
/// <param name="ExpertId">FK to the expert who created this content.</param>
/// <param name="PriceTierId">FK to the pricing tier for this video.</param>
/// <param name="Title">Full video title.</param>
/// <param name="Slug">URL slug — must be unique and lowercase with hyphens only.</param>
/// <param name="Synopsis">Short blurb for the grid card.</param>
/// <param name="Description">Long rich text for the detail page.</param>
/// <param name="CardImage">Grid card cover image URL.</param>
/// <param name="HeroImage">Detail page hero image URL.</param>
/// <param name="IconEmoji">Emoji icon for gradient card fallback.</param>
/// <param name="GradientClass">CSS gradient class for card fallback.</param>
/// <param name="TrailerUrl">YouTube embed URL for the public trailer.</param>
/// <param name="StreamUrl">YouTube embed URL for the full video (Masterclass only).</param>
/// <param name="VideoType">Content type string: "Masterclass" or "Series".</param>
/// <param name="TotalDuration">Display string for total duration, e.g. "4h 10m".</param>
/// <param name="TotalDurationSeconds">Total duration in seconds for sorting/filtering.</param>
/// <param name="ReleaseYear">Release year for display, e.g. "2026".</param>
/// <param name="IsFeatured">Whether this video appears in the featured row.</param>
/// <param name="FeaturedLabel">Eyebrow text shown on the featured card.</param>
/// <param name="FeaturedPosition">Position in the featured row.</param>
/// <param name="SortOrder">Display ordering within the category (ascending).</param>
/// <param name="Tags">Optional list of tag strings.</param>
/// <param name="Features">Optional list of feature inputs (icon + description).</param>
public record CreateLibraryVideoCommand(
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
    int SortOrder,
    List<string>? Tags,
    List<CreateVideoFeatureInput>? Features) : IRequest<Guid>;
