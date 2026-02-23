namespace FemVed.Application.Experts.DTOs;

/// <summary>
/// Response shape for a single program entry in GET /api/v1/experts/me/programs.
/// Provides the expert with a high-level view of each program's status and enrollment activity.
/// </summary>
/// <param name="ProgramId">UUID of the program.</param>
/// <param name="Name">Full program name.</param>
/// <param name="Slug">URL slug, e.g. "break-stress-hormone-health-triangle".</param>
/// <param name="Status">Current lifecycle status: Draft, PendingReview, Published, or Archived.</param>
/// <param name="GridImageUrl">Grid card image URL (may be null).</param>
/// <param name="ActiveEnrollments">Count of UserProgramAccess records with status Active.</param>
/// <param name="TotalEnrollments">Total count of UserProgramAccess records (all statuses).</param>
/// <param name="CreatedAt">UTC timestamp when the program was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the last update.</param>
public record ExpertProgramSummaryDto(
    Guid ProgramId,
    string Name,
    string Slug,
    string Status,
    string? GridImageUrl,
    int ActiveEnrollments,
    int TotalEnrollments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
