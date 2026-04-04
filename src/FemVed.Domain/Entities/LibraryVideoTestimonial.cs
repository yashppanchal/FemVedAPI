namespace FemVed.Domain.Entities;

/// <summary>
/// Admin-curated testimonial/review for a library video.
/// Similar to <see cref="ProgramTestimonial"/> for guided programs.
/// </summary>
public class LibraryVideoTestimonial
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the video.</summary>
    public Guid VideoId { get; set; }

    /// <summary>Reviewer name.</summary>
    public string ReviewerName { get; set; } = string.Empty;

    /// <summary>Review text.</summary>
    public string ReviewText { get; set; } = string.Empty;

    /// <summary>Star rating (1–5).</summary>
    public int Rating { get; set; }

    /// <summary>Display ordering (ascending).</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this testimonial is visible.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    /// <summary>The video this testimonial belongs to.</summary>
    public LibraryVideo Video { get; set; } = null!;
}
