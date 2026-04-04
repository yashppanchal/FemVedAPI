namespace FemVed.Application.Library.DTOs;

/// <summary>Response for GET /api/v1/users/me/library — the user's purchased videos with progress.</summary>
/// <param name="Videos">All purchased videos with watch progress.</param>
public record MyLibraryResponse(List<MyLibraryVideoDto> Videos);

/// <summary>A purchased video in the user's library.</summary>
/// <param name="VideoId">Video primary key.</param>
/// <param name="Title">Video title.</param>
/// <param name="Slug">URL slug.</param>
/// <param name="CardImage">Cover image URL.</param>
/// <param name="IconEmoji">Emoji icon for gradient fallback.</param>
/// <param name="GradientClass">CSS gradient class.</param>
/// <param name="VideoType">Content type: "MASTERCLASS" or "SERIES".</param>
/// <param name="TotalDuration">Display duration.</param>
/// <param name="EpisodeCount">Number of episodes (null for Masterclass).</param>
/// <param name="ExpertName">Expert display name.</param>
/// <param name="PurchasedAt">UTC purchase timestamp.</param>
/// <param name="WatchProgressSeconds">Overall watch progress in seconds.</param>
/// <param name="LastWatchedAt">UTC timestamp of last watch, or null.</param>
/// <param name="CategorySlug">Category slug for building the detail page URL.</param>
public record MyLibraryVideoDto(
    Guid VideoId,
    string Title,
    string Slug,
    string? CardImage,
    string? IconEmoji,
    string? GradientClass,
    string VideoType,
    string? TotalDuration,
    int? EpisodeCount,
    string ExpertName,
    DateTimeOffset PurchasedAt,
    int WatchProgressSeconds,
    DateTimeOffset? LastWatchedAt,
    string CategorySlug);
