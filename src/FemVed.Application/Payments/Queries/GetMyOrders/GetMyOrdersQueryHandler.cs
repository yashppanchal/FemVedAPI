using FemVed.Application.Interfaces;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Queries.GetMyOrders;

/// <summary>
/// Handles <see cref="GetMyOrdersQuery"/>.
/// Returns all orders for the requesting user, ordered by creation date descending.
/// </summary>
public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, List<OrderDto>>
{
    private readonly IRepository<Order> _orders;
    private readonly ILogger<GetMyOrdersQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetMyOrdersQueryHandler(
        IRepository<Order> orders,
        ILogger<GetMyOrdersQueryHandler> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    /// <summary>Returns all orders for the authenticated user.</summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of order DTOs, newest first.</returns>
    public async Task<List<OrderDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching orders for user {UserId}", request.UserId);

        var orders = await _orders.GetAllAsync(
            o => o.UserId == request.UserId,
            cancellationToken);

        var result = orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(MapToDto)
            .ToList();

        _logger.LogInformation("Returned {Count} orders for user {UserId}", result.Count, request.UserId);

        return result;
    }

    private static OrderDto MapToDto(Order order) =>
        new(
            OrderId: order.Id,
            UserId: order.UserId,
            DurationId: order.DurationId,
            AmountPaid: order.AmountPaid,
            CurrencyCode: order.CurrencyCode,
            LocationCode: order.LocationCode,
            DiscountAmount: order.DiscountAmount,
            Status: order.Status.ToString(),
            Gateway: order.PaymentGateway.ToString().ToUpperInvariant(),
            GatewayOrderId: order.GatewayOrderId,
            FailureReason: order.FailureReason,
            CreatedAt: order.CreatedAt);
}
