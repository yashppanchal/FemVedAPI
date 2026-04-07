using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetExpertLibrarySales;

/// <summary>Returns library video sales data for the authenticated expert.</summary>
/// <param name="UserId">The authenticated user's ID (from JWT).</param>
public record GetExpertLibrarySalesQuery(Guid UserId) : IRequest<ExpertLibrarySalesResponse>;
