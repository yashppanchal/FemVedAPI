using MediatR;

namespace FemVed.Application.Enrollments.Commands.StartEnrollment;

/// <summary>
/// Transitions a <c>NOT_STARTED</c> enrollment to <c>ACTIVE</c>, or schedules it for a future date.
/// Only experts (for their own programs) and admins may start an enrollment.
/// </summary>
/// <param name="AccessId">UUID of the <c>UserProgramAccess</c> record to start.</param>
/// <param name="PerformedByUserId">Authenticated user's ID (expert user ID or admin user ID).</param>
/// <param name="IsAdmin">True when the caller holds the Admin role.</param>
/// <param name="Note">Optional reason / message logged to <c>program_session_log</c>.</param>
/// <param name="ScheduledDate">
/// Optional future date to schedule the program start.
/// Null or today = start immediately. Future date = schedule and email all parties.
/// </param>
public record StartEnrollmentCommand(
    Guid AccessId,
    Guid PerformedByUserId,
    bool IsAdmin,
    string? Note,
    DateOnly? ScheduledDate = null) : IRequest;
