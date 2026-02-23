using FemVed.Application.Payments.DTOs;
using MediatR;

namespace FemVed.Application.Payments.Queries.GetMyOrders;

/// <summary>
/// Returns all orders belonging to the authenticated user, ordered newest first.
/// </summary>
/// <param name="UserId">Authenticated user's ID.</param>
public record GetMyOrdersQuery(Guid UserId) : IRequest<List<OrderDto>>;
