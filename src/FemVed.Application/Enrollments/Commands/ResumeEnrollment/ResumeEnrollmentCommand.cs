using MediatR;

namespace FemVed.Application.Enrollments.Commands.ResumeEnrollment;

/// <summary>
/// Transitions a <c>PAUSED</c> enrollment back to <c>ACTIVE</c>.
/// Only experts (own programs) and admins may resume an enrollment.
/// </summary>
/// <param name="AccessId">UUID of the <c>UserProgramAccess</c> record to resume.</param>
/// <param name="PerformedByUserId">Authenticated user's ID.</param>
/// <param name="IsAdmin">True when the caller holds the Admin role.</param>
/// <param name="Note">Optional reason logged to <c>program_session_log</c>.</param>
public record ResumeEnrollmentCommand(
    Guid AccessId,
    Guid PerformedByUserId,
    bool IsAdmin,
    string? Note) : IRequest;
