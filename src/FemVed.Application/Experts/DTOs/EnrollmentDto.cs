namespace FemVed.Application.Experts.DTOs;

/// <summary>
/// Response shape for a single enrollment entry returned by GET /api/v1/experts/me/enrollments
/// and GET /api/v1/admin/enrollments.
/// Contains everything needed to identify an enrolled user, their program access, and scheduling state.
/// </summary>
/// <param name="AccessId">UUID of the UserProgramAccess record (used for session actions and progress updates).</param>
/// <param name="OrderId">UUID of the order that granted this access.</param>
/// <param name="UserId">UUID of the enrolled user.</param>
/// <param name="UserFirstName">Enrolled user's first name.</param>
/// <param name="UserLastName">Enrolled user's last name.</param>
/// <param name="UserEmail">Enrolled user's email address.</param>
/// <param name="ProgramId">UUID of the program.</param>
/// <param name="ProgramName">Display name of the program.</param>
/// <param name="DurationLabel">Human-readable duration, e.g. "6 weeks".</param>
/// <param name="DurationWeeks">Numeric duration in weeks — used by the frontend to calculate projected end dates for scheduled programs.</param>
/// <param name="AccessStatus">Current access state: NotStarted, Active, Paused, Completed, or Cancelled.</param>
/// <param name="StartedAt">UTC timestamp when the expert started the program (null if not yet started).</param>
/// <param name="PausedAt">UTC timestamp when the program was last paused (null if never paused or since resumed).</param>
/// <param name="CompletedAt">UTC timestamp when the program was completed or ended (null if not completed).</param>
/// <param name="EndedBy">UUID of the user who triggered the end action (null if not yet ended).</param>
/// <param name="EndedByRole">Role of the user who ended the program: EXPERT, ADMIN, or USER (null if not yet ended).</param>
/// <param name="EnrolledAt">UTC timestamp when the access was granted (order paid).</param>
/// <param name="ExpertId">UUID of the expert delivering the program.</param>
/// <param name="ExpertName">Display name of the expert (null if expert record not found).</param>
/// <param name="ScheduledStartAt">UTC timestamp when the program is scheduled to auto-start (null if not scheduled).</param>
public record EnrollmentDto(
    Guid AccessId,
    Guid OrderId,
    Guid UserId,
    string UserFirstName,
    string UserLastName,
    string UserEmail,
    Guid ProgramId,
    string ProgramName,
    string DurationLabel,
    int DurationWeeks,
    string AccessStatus,
    DateTimeOffset? StartedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? CompletedAt,
    Guid? EndedBy,
    string? EndedByRole,
    DateTimeOffset EnrolledAt,
    Guid ExpertId,
    string? ExpertName,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? EndDate,
    DateTimeOffset? RequestedStartDate,
    string? StartRequestStatus);
