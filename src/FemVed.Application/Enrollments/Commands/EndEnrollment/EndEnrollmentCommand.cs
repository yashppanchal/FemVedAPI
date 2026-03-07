using MediatR;

namespace FemVed.Application.Enrollments.Commands.EndEnrollment;

/// <summary>
/// Transitions an <c>ACTIVE</c> or <c>PAUSED</c> enrollment to <c>COMPLETED</c>.
/// Experts (own programs), admins, and the enrolled user may all end an enrollment.
/// </summary>
/// <param name="AccessId">UUID of the <c>UserProgramAccess</c> record to end.</param>
/// <param name="PerformedByUserId">Authenticated user's ID.</param>
/// <param name="IsAdmin">True when the caller holds the Admin role.</param>
/// <param name="IsUser">True when the caller is the enrolled user (not expert, not admin).</param>
/// <param name="Note">Optional reason logged to <c>program_session_log</c>.</param>
public record EndEnrollmentCommand(
    Guid AccessId,
    Guid PerformedByUserId,
    bool IsAdmin,
    bool IsUser,
    string? Note) : IRequest;
