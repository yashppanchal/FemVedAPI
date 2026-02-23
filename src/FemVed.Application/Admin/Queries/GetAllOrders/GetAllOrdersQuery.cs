using FemVed.Application.Payments.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAllOrders;

/// <summary>Returns all orders across all users, ordered by creation date descending.</summary>
public record GetAllOrdersQuery : IRequest<List<OrderDto>>;
