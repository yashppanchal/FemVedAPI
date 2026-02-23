using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>
/// Records a user's active access to a program after a successful purchase.
/// Created automatically via the <c>OrderPaidEvent</c> domain event.
/// </summary>
public class UserProgramAccess
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the user who purchased the program.</summary>
    public Guid UserId { get; set; }

    /// <summary>FK to the order that granted this access.</summary>
    public Guid OrderId { get; set; }

    /// <summary>FK to the program the user now has access to.</summary>
    public Guid ProgramId { get; set; }

    /// <summary>FK to the specific duration the user purchased.</summary>
    public Guid DurationId { get; set; }

    /// <summary>FK to the expert delivering the program.</summary>
    public Guid ExpertId { get; set; }

    /// <summary>Current access state.</summary>
    public UserProgramAccessStatus Status { get; set; } = UserProgramAccessStatus.Active;

    /// <summary>Whether the 24-hour program reminder has been sent.</summary>
    public bool ReminderSent { get; set; }

    /// <summary>UTC timestamp when the user started the program.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>UTC timestamp when the user completed the program.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The user with access.</summary>
    public User User { get; set; } = null!;

    /// <summary>The order that created this access record.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>The program this access is for.</summary>
    public Program Program { get; set; } = null!;

    /// <summary>The duration purchased.</summary>
    public ProgramDuration Duration { get; set; } = null!;

    /// <summary>The expert delivering the program.</summary>
    public Expert Expert { get; set; } = null!;

    /// <summary>Progress updates sent by the expert to this user.</summary>
    public ICollection<ExpertProgressUpdate> ProgressUpdates { get; set; } = new List<ExpertProgressUpdate>();
}
