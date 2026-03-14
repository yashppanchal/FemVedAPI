using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAdminExpertPrograms;

/// <summary>
/// Returns a summary of all programs belonging to a specific expert,
/// including total and active enrollment counts per program.
/// Used by the admin Experts tab to show program breakdowns inline.
/// </summary>
/// <param name="ExpertId">UUID of the expert whose programs to retrieve.</param>
public record GetAdminExpertProgramsQuery(Guid ExpertId) : IRequest<List<ExpertProgramSummaryDto>>;
