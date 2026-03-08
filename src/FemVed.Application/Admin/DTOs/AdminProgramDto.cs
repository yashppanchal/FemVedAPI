namespace FemVed.Application.Admin.DTOs;

/// <summary>
/// Flat program record for the admin program listing endpoint.
/// Includes status, expert name, and category name so the admin can filter and triage
/// without having to navigate into each program individually.
/// </summary>
/// <param name="ProgramId">UUID of the program.</param>
/// <param name="Name">Full program name.</param>
/// <param name="Slug">URL slug.</param>
/// <param name="Status">Lifecycle status: Draft, PendingReview, Published, or Archived.</param>
/// <param name="IsActive">Whether the program is visible in catalog queries.</param>
/// <param name="IsDeleted">Soft-delete flag.</param>
/// <param name="ExpertId">UUID of the expert who owns this program.</param>
/// <param name="ExpertName">Display name of the expert (first + last from linked user account).</param>
/// <param name="CategoryId">UUID of the parent category.</param>
/// <param name="CategoryName">Display name of the parent category.</param>
/// <param name="SortOrder">Display ordering within the category.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="UpdatedAt">UTC last-update timestamp.</param>
public record AdminProgramDto(
    Guid ProgramId,
    string? Name,
    string Slug,
    string Status,
    bool IsActive,
    bool IsDeleted,
    Guid ExpertId,
    string ExpertName,
    Guid CategoryId,
    string CategoryName,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
