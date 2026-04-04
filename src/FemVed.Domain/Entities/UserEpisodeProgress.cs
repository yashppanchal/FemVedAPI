namespace FemVed.Domain.Entities;

/// <summary>
/// Per-episode watch progress for a user within a Series-type library video.
/// </summary>
public class UserEpisodeProgress
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the user.</summary>
    public Guid UserId { get; set; }

    /// <summary>FK to the episode.</summary>
    public Guid EpisodeId { get; set; }

    /// <summary>Watch progress in seconds.</summary>
    public int WatchProgressSeconds { get; set; }

    /// <summary>Whether the user has completed this episode.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>UTC timestamp of last watch.</summary>
    public DateTimeOffset? LastWatchedAt { get; set; }

    // Navigations
    /// <summary>The user.</summary>
    public User User { get; set; } = null!;

    /// <summary>The episode.</summary>
    public LibraryVideoEpisode Episode { get; set; } = null!;
}
