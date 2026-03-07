using MediatR;

namespace FemVed.Application.Enrollments.Commands.PauseEnrollment;

/// <summary>
/// Transitions an <c>ACTIVE</c> enrollment to <c>PAUSED</c>.
/// Experts (own programs), admins, and the enrolled user may all pause.
/// </summary>
/// <param name="AccessId">UUID of the <c>UserProgramAccess</c> record to pause.</param>
/// <param name="PerformedByUserId">Authenticated user's ID.</param>
/// <param name="IsAdmin">True when the caller holds the Admin role.</param>
/// <param name="IsUser">True when the caller is the enrolled user (not expert, not admin).</param>
/// <param name="Note">Optional reason logged to <c>program_session_log</c>.</param>
public record PauseEnrollmentCommand(
    Guid AccessId,
    Guid PerformedByUserId,
    bool IsAdmin,
    bool IsUser,
    string? Note) : IRequest;
