namespace FemVed.Domain.Entities;

/// <summary>
/// User-submitted review for a purchased library video.
/// Users can only review videos they have purchased.
/// </summary>
public class UserVideoReview
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the user who submitted the review.</summary>
    public Guid UserId { get; set; }

    /// <summary>FK to the reviewed video.</summary>
    public Guid VideoId { get; set; }

    /// <summary>Star rating (1–5).</summary>
    public int Rating { get; set; }

    /// <summary>Review text. Null if user only rated without writing.</summary>
    public string? ReviewText { get; set; }

    /// <summary>Whether this review is visible (admin can moderate).</summary>
    public bool IsApproved { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The user who submitted this review.</summary>
    public User User { get; set; } = null!;

    /// <summary>The reviewed video.</summary>
    public LibraryVideo Video { get; set; } = null!;
}
