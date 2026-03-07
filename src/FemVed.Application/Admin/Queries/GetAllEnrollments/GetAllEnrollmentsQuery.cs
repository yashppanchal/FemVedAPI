using FemVed.Application.Experts.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAllEnrollments;

/// <summary>
/// Returns all user program access records (enrollments) across all experts and programs,
/// ordered by enrollment date descending. Required by admin to discover accessIds for
/// session management actions (start, pause, resume, end).
/// </summary>
/// <param name="StatusFilter">
/// Optional access-status filter, e.g. "Active". Null = return all statuses.
/// </param>
public record GetAllEnrollmentsQuery(string? StatusFilter = null) : IRequest<List<EnrollmentDto>>;
