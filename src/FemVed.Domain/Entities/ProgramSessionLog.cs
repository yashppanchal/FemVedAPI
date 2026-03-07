using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>
/// Audit record written each time a session lifecycle action is performed
/// (start / pause / resume / end) on a <see cref="UserProgramAccess"/> record.
/// </summary>
public class ProgramSessionLog
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the enrollment record this log entry belongs to.</summary>
    public Guid AccessId { get; set; }

    /// <summary>The lifecycle action that was performed.</summary>
    public SessionAction Action { get; set; }

    /// <summary>FK to the user who performed the action.</summary>
    public Guid PerformedBy { get; set; }

    /// <summary>Role of the user who performed the action: EXPERT, ADMIN, or USER.</summary>
    public string PerformedByRole { get; set; } = string.Empty;

    /// <summary>Optional note or reason provided when the action was triggered.</summary>
    public string? Note { get; set; }

    /// <summary>UTC timestamp when this log entry was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigations
    /// <summary>The enrollment record this log entry belongs to.</summary>
    public UserProgramAccess Access { get; set; } = null!;
}
