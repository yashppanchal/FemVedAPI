namespace FemVed.Application.Library.DTOs;

// ── Matches the Stream URL Response JSON contract in WELLNESS_LIBRARY_PROMPT.md §4.
// Only returned to authenticated users who have purchased the video.

/// <summary>Stream URL response for GET /api/v1/library/videos/{slug}/stream.</summary>
/// <param name="VideoId">Video primary key.</param>
/// <param name="VideoType">Content type: "MASTERCLASS" or "SERIES".</param>
/// <param name="StreamUrl">YouTube embed URL for Masterclass, or null for Series.</param>
/// <param name="Episodes">Episode list with stream URLs (Series only). Empty for Masterclass.</param>
/// <param name="OverallProgressSeconds">Total watch progress in seconds.</param>
/// <param name="LastWatchedAt">UTC timestamp of last watch, or null.</param>
public record LibraryStreamResponse(
    Guid VideoId,
    string VideoType,
    string? StreamUrl,
    List<LibraryStreamEpisodeDto> Episodes,
    int OverallProgressSeconds,
    DateTimeOffset? LastWatchedAt);

/// <summary>An episode with its stream URL and watch progress (for purchased users).</summary>
/// <param name="EpisodeId">Episode primary key.</param>
/// <param name="EpisodeNumber">1-based episode number.</param>
/// <param name="Title">Episode title.</param>
/// <param name="StreamUrl">YouTube embed URL for this episode.</param>
/// <param name="WatchProgressSeconds">Per-episode watch progress in seconds.</param>
/// <param name="IsCompleted">Whether the user has completed this episode.</param>
public record LibraryStreamEpisodeDto(
    Guid EpisodeId,
    int EpisodeNumber,
    string Title,
    string? StreamUrl,
    int WatchProgressSeconds,
    bool IsCompleted);
