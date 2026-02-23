using FemVed.Application.Interfaces;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Queries.GetOrder;

/// <summary>
/// Handles <see cref="GetOrderQuery"/>.
/// Returns the order DTO, enforcing ownership rules for non-Admin callers.
/// </summary>
public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto>
{
    private readonly IRepository<Order> _orders;
    private readonly ILogger<GetOrderQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetOrderQueryHandler(
        IRepository<Order> orders,
        ILogger<GetOrderQueryHandler> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    /// <summary>Returns the order DTO for the given order ID.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the order is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when a non-Admin user requests another user's order.</exception>
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching order {OrderId} for user {UserId}", request.OrderId, request.RequestingUserId);

        var order = await _orders.FirstOrDefaultAsync(
            o => o.Id == request.OrderId,
            cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (!request.IsAdmin && order.UserId != request.RequestingUserId)
            throw new ForbiddenException("You do not have permission to view this order.");

        _logger.LogInformation("Order {OrderId} returned successfully", request.OrderId);

        return MapToDto(order);
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
