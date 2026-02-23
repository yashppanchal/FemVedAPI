namespace FemVed.Application.Experts.DTOs;

/// <summary>
/// Response shape for a single enrollment entry returned by GET /api/v1/experts/me/enrollments.
/// Contains everything an expert needs to identify an enrolled user and their program access.
/// </summary>
/// <param name="AccessId">UUID of the UserProgramAccess record (used for sending progress updates).</param>
/// <param name="OrderId">UUID of the order that granted this access.</param>
/// <param name="UserId">UUID of the enrolled user.</param>
/// <param name="UserFirstName">Enrolled user's first name.</param>
/// <param name="UserLastName">Enrolled user's last name.</param>
/// <param name="UserEmail">Enrolled user's email address.</param>
/// <param name="ProgramId">UUID of the program.</param>
/// <param name="ProgramName">Display name of the program.</param>
/// <param name="DurationLabel">Human-readable duration, e.g. "6 weeks".</param>
/// <param name="AccessStatus">Current access state: Active, Expired, or Revoked.</param>
/// <param name="StartedAt">UTC timestamp when the user started the program (null if not yet started).</param>
/// <param name="CompletedAt">UTC timestamp when the user completed the program (null if not completed).</param>
/// <param name="EnrolledAt">UTC timestamp when the access was granted (order paid).</param>
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
    string AccessStatus,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset EnrolledAt);
