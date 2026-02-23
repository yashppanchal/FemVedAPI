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
/// <param name="Status">Access state: Active, Expired, or Revoked.</param>
/// <param name="StartedAt">UTC timestamp when the user started the program (null if not yet started).</param>
/// <param name="CompletedAt">UTC timestamp when the user completed the program (null if not completed).</param>
/// <param name="PurchasedAt">UTC timestamp when access was granted (order paid).</param>
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
    DateTimeOffset? CompletedAt,
    DateTimeOffset PurchasedAt);
