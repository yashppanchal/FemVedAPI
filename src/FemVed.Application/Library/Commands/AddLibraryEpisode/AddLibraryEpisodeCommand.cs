using MediatR;

namespace FemVed.Application.Library.Commands.AddLibraryEpisode;

/// <summary>
/// Creates a new episode for a Series-type library video.
/// </summary>
/// <param name="VideoId">The parent video's ID.</param>
/// <param name="EpisodeNumber">Episode number within the series (1-based).</param>
/// <param name="Title">Episode title.</param>
/// <param name="Description">Optional episode description.</param>
/// <param name="Duration">Display string for duration, e.g. "18 min".</param>
/// <param name="DurationSeconds">Duration in seconds for progress tracking.</param>
/// <param name="StreamUrl">YouTube embed URL for this episode.</param>
/// <param name="ThumbnailUrl">Thumbnail image URL.</param>
/// <param name="IsFreePreview">Whether this episode is available as a free preview.</param>
/// <param name="SortOrder">Display ordering (ascending).</param>
public record AddLibraryEpisodeCommand(
    Guid VideoId,
    int EpisodeNumber,
    string Title,
    string? Description,
    string? Duration,
    int? DurationSeconds,
    string? StreamUrl,
    string? ThumbnailUrl,
    bool IsFreePreview,
    int SortOrder) : IRequest<Guid>;
