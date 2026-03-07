using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetProgramAnalytics;

/// <summary>
/// Returns per-program sales and enrollment stats and per-expert revenue summaries
/// (including expert share and platform commission). Used for the admin program dashboard.
/// </summary>
public record GetProgramAnalyticsQuery : IRequest<ProgramAnalyticsDto>;
