using MediatR;

namespace FemVed.Application.Library.Commands.UpdateLibraryEpisode;

/// <summary>
/// Updates an existing library video episode. Only non-null fields are applied.
/// </summary>
/// <param name="EpisodeId">The episode's primary key.</param>
/// <param name="EpisodeNumber">New episode number (optional).</param>
/// <param name="Title">New title (optional).</param>
/// <param name="Description">New description (optional).</param>
/// <param name="Duration">New display duration string (optional).</param>
/// <param name="DurationSeconds">New duration in seconds (optional).</param>
/// <param name="StreamUrl">New stream URL (optional).</param>
/// <param name="ThumbnailUrl">New thumbnail URL (optional).</param>
/// <param name="IsFreePreview">New free-preview flag (optional).</param>
/// <param name="SortOrder">New sort order (optional).</param>
public record UpdateLibraryEpisodeCommand(
    Guid EpisodeId,
    int? EpisodeNumber,
    string? Title,
    string? Description,
    string? Duration,
    int? DurationSeconds,
    string? StreamUrl,
    string? ThumbnailUrl,
    bool? IsFreePreview,
    int? SortOrder) : IRequest;
