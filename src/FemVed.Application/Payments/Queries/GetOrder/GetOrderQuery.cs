using FemVed.Application.Payments.DTOs;
using MediatR;

namespace FemVed.Application.Payments.Queries.GetOrder;

/// <summary>
/// Returns a single order by its ID.
/// Users may only retrieve their own orders; Admins may retrieve any order.
/// </summary>
/// <param name="OrderId">The order to retrieve.</param>
/// <param name="RequestingUserId">Authenticated user's ID (used for ownership check).</param>
/// <param name="IsAdmin">True when the caller is an Admin (bypasses ownership check).</param>
public record GetOrderQuery(Guid OrderId, Guid RequestingUserId, bool IsAdmin) : IRequest<OrderDto>;
