namespace FemVed.Application.Users.DTOs;

/// <summary>
/// Response shape for a single program access record returned by GET /api/v1/users/me/program-access.
/// </summary>
/// <param name="AccessId">UUID of the UserProgramAccess record.</param>
/// <param name="OrderId">UUID of the order that granted this access.</param>
/// <param name="ProgramId">UUID of the program.</param>
/// <param name="ProgramName">Display name of the program.</param>
/// <param name="ProgramImageUrl">Grid card image URL (may be null).</param>
/// <param name="ExpertId">UUID of the delivering expert.</param>
/// <param name="ExpertName">Expert's public display name.</param>
/// <param name="DurationLabel">Human-readable duration, e.g. "6 weeks".</param>
/// <param name="Status">Access state: NotStarted, Active, Paused, Completed, or Cancelled.</param>
/// <param name="StartedAt">UTC timestamp when the expert started the program (null if not yet started).</param>
/// <param name="PausedAt">UTC timestamp when the program was last paused (null if never paused or since resumed).</param>
/// <param name="CompletedAt">UTC timestamp when the program was completed or ended (null if not completed).</param>
/// <param name="PurchasedAt">UTC timestamp when access was granted (order paid).</param>
/// <param name="ScheduledStartAt">UTC timestamp when the program is scheduled to auto-start (null if not scheduled).</param>
/// <param name="EndDate">Calculated end date: StartedAt plus duration in weeks, extended by any pause time (null until started).</param>
/// <param name="RequestedStartDate">User's preferred start date submitted via request-start (null if none submitted).</param>
/// <param name="StartRequestStatus">Status of the start date request: Pending, Approved, or Declined (null if no request).</param>
public record ProgramAccessDto(
    Guid AccessId,
    Guid OrderId,
    Guid ProgramId,
    string ProgramName,
    string? ProgramImageUrl,
    Guid ExpertId,
    string ExpertName,
    string DurationLabel,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset PurchasedAt,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? EndDate,
    DateTimeOffset? RequestedStartDate,
    string? StartRequestStatus);
