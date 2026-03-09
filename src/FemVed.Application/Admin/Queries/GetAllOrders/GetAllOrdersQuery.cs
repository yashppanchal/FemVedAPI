using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAllOrders;

/// <summary>Returns all orders across all users, enriched with user name, program name, duration label, and coupon code. Ordered by creation date descending.</summary>
public record GetAllOrdersQuery : IRequest<List<AdminOrderDto>>;
