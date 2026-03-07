using FemVed.Application.Payments.DTOs;
using MediatR;

namespace FemVed.Application.Payments.Queries.GetMyRefunds;

/// <summary>
/// Returns all refund records for orders belonging to the authenticated user, newest first.
/// </summary>
/// <param name="UserId">Authenticated user's ID.</param>
public record GetMyRefundsQuery(Guid UserId) : IRequest<List<RefundDto>>;
