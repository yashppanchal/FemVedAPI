using FemVed.Application.Library.Commands.CreateLibraryVideo;
using MediatR;

namespace FemVed.Application.Library.Commands.UpdateLibraryVideo;

/// <summary>
/// Updates an existing library video. Only non-null fields are applied.
/// If Tags or Features are provided, existing children are replaced entirely.
/// </summary>
/// <param name="VideoId">The video to update.</param>
/// <param name="CategoryId">New category FK (null to keep current).</param>
/// <param name="PriceTierId">New price tier FK (null to keep current).</param>
/// <param name="Title">New title (null to keep current).</param>
/// <param name="Slug">New slug (null to keep current). Uniqueness checked if changed.</param>
/// <param name="Synopsis">New synopsis (null to keep current).</param>
/// <param name="Description">New description (null to keep current).</param>
/// <param name="CardImage">New card image URL (null to keep current).</param>
/// <param name="HeroImage">New hero image URL (null to keep current).</param>
/// <param name="IconEmoji">New icon emoji (null to keep current).</param>
/// <param name="GradientClass">New gradient class (null to keep current).</param>
/// <param name="TrailerUrl">New trailer URL (null to keep current).</param>
/// <param name="StreamUrl">New stream URL (null to keep current).</param>
/// <param name="TotalDuration">New total duration string (null to keep current).</param>
/// <param name="TotalDurationSeconds">New total duration in seconds (null to keep current).</param>
/// <param name="ReleaseYear">New release year (null to keep current).</param>
/// <param name="IsFeatured">New featured flag (null to keep current).</param>
/// <param name="FeaturedLabel">New featured label (null to keep current).</param>
/// <param name="FeaturedPosition">New featured position (null to keep current).</param>
/// <param name="SortOrder">New sort order (null to keep current).</param>
/// <param name="Tags">Replacement tag list (null to keep existing tags).</param>
/// <param name="Features">Replacement feature list (null to keep existing features).</param>
public record UpdateLibraryVideoCommand(
    Guid VideoId,
    Guid? CategoryId,
    Guid? PriceTierId,
    string? Title,
    string? Slug,
    string? Synopsis,
    string? Description,
    string? CardImage,
    string? HeroImage,
    string? IconEmoji,
    string? GradientClass,
    string? TrailerUrl,
    string? StreamUrl,
    string? TotalDuration,
    int? TotalDurationSeconds,
    string? ReleaseYear,
    bool? IsFeatured,
    string? FeaturedLabel,
    int? FeaturedPosition,
    int? SortOrder,
    List<string>? Tags,
    List<CreateVideoFeatureInput>? Features) : IRequest;
