using MediatR;

namespace FemVed.Application.Guided.Commands.DeleteDuration;

/// <summary>
/// Deactivates a program duration (sets <c>IsActive = false</c>).
/// All associated prices remain in the database but will not be shown in the public catalog.
/// Experts may only deactivate durations on their own DRAFT or PENDING_REVIEW programs.
/// Admins may deactivate any duration at any program status.
/// </summary>
/// <param name="DurationId">The duration to deactivate.</param>
/// <param name="ProgramId">The program that owns this duration (used for ownership verification).</param>
/// <param name="RequestingUserId">Authenticated user ID.</param>
/// <param name="IsAdmin">True when the caller has the Admin role.</param>
public record DeleteDurationCommand(
    Guid DurationId,
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest;
