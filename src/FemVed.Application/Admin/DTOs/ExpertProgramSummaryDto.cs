namespace FemVed.Application.Admin.DTOs;

/// <summary>
/// Summary of a single program belonging to a specific expert, including enrollment counts.
/// Returned by GET /api/v1/admin/experts/{expertId}/programs.
/// </summary>
/// <param name="ProgramId">UUID of the program.</param>
/// <param name="ProgramName">Display name of the program (null if not yet set).</param>
/// <param name="Status">Lifecycle status: Draft, PendingReview, Published, or Archived.</param>
/// <param name="TotalEnrollments">Total number of UserProgramAccess records for this program.</param>
/// <param name="ActiveEnrollments">Number of enrollments currently in Active status.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
public record ExpertProgramSummaryDto(
    Guid ProgramId,
    string? ProgramName,
    string Status,
    int TotalEnrollments,
    int ActiveEnrollments,
    DateTimeOffset CreatedAt);
