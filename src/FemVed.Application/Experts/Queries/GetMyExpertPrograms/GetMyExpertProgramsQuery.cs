using FemVed.Application.Experts.DTOs;
using MediatR;

namespace FemVed.Application.Experts.Queries.GetMyExpertPrograms;

/// <summary>
/// Returns a summary of all programs (all statuses) belonging to the authenticated expert,
/// including per-program enrollment counts. Ordered by creation date descending.
/// </summary>
/// <param name="ExpertId">The authenticated expert's ID.</param>
public record GetMyExpertProgramsQuery(Guid ExpertId) : IRequest<List<ExpertProgramSummaryDto>>;
