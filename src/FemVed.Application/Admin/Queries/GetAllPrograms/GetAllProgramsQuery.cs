using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAllPrograms;

/// <summary>
/// Returns all programs across all experts and statuses (including soft-deleted ones),
/// ordered by creation date descending. Used by the admin dashboard for review workflow —
/// the only way for an admin to discover programs in PENDING_REVIEW status.
/// </summary>
/// <param name="StatusFilter">
/// Optional status filter, e.g. "PendingReview". Null = return all statuses.
/// </param>
public record GetAllProgramsQuery(string? StatusFilter = null) : IRequest<List<AdminProgramDto>>;
