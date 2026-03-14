namespace FemVed.Domain.Enums;

/// <summary>
/// Lifecycle state of a user's requested start date for an enrollment.
/// </summary>
public enum StartRequestStatus
{
    /// <summary>User has submitted a preferred start date and it is awaiting expert/admin review.</summary>
    Pending,

    /// <summary>Expert or admin approved the requested date; ScheduledStartAt has been set.</summary>
    Approved,

    /// <summary>Expert or admin declined the requested date.</summary>
    Declined
}
