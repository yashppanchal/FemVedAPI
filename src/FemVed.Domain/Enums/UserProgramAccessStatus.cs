namespace FemVed.Domain.Enums;

/// <summary>State of a user's access to a purchased program.</summary>
public enum UserProgramAccessStatus
{
    /// <summary>Purchase complete but the expert has not started the program yet.</summary>
    NotStarted,

    /// <summary>Program is currently in progress.</summary>
    Active,

    /// <summary>Program is temporarily on hold.</summary>
    Paused,

    /// <summary>Program has been completed.</summary>
    Completed,

    /// <summary>Access revoked (refund / admin cancellation).</summary>
    Cancelled
}
