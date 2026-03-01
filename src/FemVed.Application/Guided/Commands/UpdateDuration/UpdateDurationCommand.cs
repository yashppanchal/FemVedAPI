using MediatR;

namespace FemVed.Application.Guided.Commands.UpdateDuration;

/// <summary>
/// Updates label, weeks, and/or sort order of an existing duration.
/// Only non-null fields are applied (partial update).
/// Experts may only update durations on their own DRAFT or PENDING_REVIEW programs.
/// Admins may update any program at any status.
/// </summary>
/// <param name="DurationId">The duration to update.</param>
/// <param name="ProgramId">The program that owns this duration (used for ownership verification).</param>
/// <param name="RequestingUserId">Authenticated user ID.</param>
/// <param name="IsAdmin">True when the caller has the Admin role.</param>
/// <param name="Label">New label, e.g. "4 weeks". Null to leave unchanged.</param>
/// <param name="Weeks">New week count. Null to leave unchanged.</param>
/// <param name="SortOrder">New sort order. Null to leave unchanged.</param>
public record UpdateDurationCommand(
    Guid DurationId,
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    string? Label,
    short? Weeks,
    int? SortOrder) : IRequest;
