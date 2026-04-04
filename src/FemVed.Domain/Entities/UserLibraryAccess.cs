namespace FemVed.Domain.Entities;

/// <summary>
/// Post-purchase access record for a library video.
/// Created when an order for a library video is paid.
/// Default: lifetime access (expires_at is null).
/// </summary>
public class UserLibraryAccess
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the user who purchased.</summary>
    public Guid UserId { get; set; }

    /// <summary>FK to the purchased video.</summary>
    public Guid VideoId { get; set; }

    /// <summary>FK to the order that granted this access.</summary>
    public Guid OrderId { get; set; }

    /// <summary>UTC timestamp of purchase.</summary>
    public DateTimeOffset PurchasedAt { get; set; }

    /// <summary>Optional expiry. Null = lifetime access (default).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Whether this access is currently active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp of last video watch.</summary>
    public DateTimeOffset? LastWatchedAt { get; set; }

    /// <summary>Overall watch progress in seconds (for Masterclass) or total across episodes (for Series).</summary>
    public int WatchProgressSeconds { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigations
    /// <summary>The user who has access.</summary>
    public User User { get; set; } = null!;

    /// <summary>The purchased video.</summary>
    public LibraryVideo Video { get; set; } = null!;

    /// <summary>The order that granted access.</summary>
    public Order Order { get; set; } = null!;
}
