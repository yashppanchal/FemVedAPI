namespace FemVed.Domain.Entities;

/// <summary>
/// Individual episode within a Series-type <see cref="LibraryVideo"/>.
/// Each episode has its own YouTube embed URL, gated behind purchase.
/// </summary>
public class LibraryVideoEpisode
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the parent video.</summary>
    public Guid VideoId { get; set; }

    /// <summary>Episode number within the series (1-based).</summary>
    public int EpisodeNumber { get; set; }

    /// <summary>Episode title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Episode description.</summary>
    public string? Description { get; set; }

    /// <summary>Display string for episode duration, e.g. "18 min".</summary>
    public string? Duration { get; set; }

    /// <summary>Duration in seconds for progress tracking.</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>YouTube embed URL for this episode (gated behind purchase).</summary>
    public string? StreamUrl { get; set; }

    /// <summary>Thumbnail image URL for this episode.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Whether this episode is available as a free preview (stream URL returned without purchase).</summary>
    public bool IsFreePreview { get; set; }

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    /// <summary>The parent video.</summary>
    public LibraryVideo Video { get; set; } = null!;

    /// <summary>Per-user watch progress for this episode.</summary>
    public ICollection<UserEpisodeProgress> UserProgress { get; set; } = new List<UserEpisodeProgress>();
}
