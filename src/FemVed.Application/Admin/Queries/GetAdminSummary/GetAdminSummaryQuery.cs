using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAdminSummary;

/// <summary>Returns aggregated platform statistics for the admin dashboard summary card.</summary>
public record GetAdminSummaryQuery : IRequest<AdminSummaryDto>;
