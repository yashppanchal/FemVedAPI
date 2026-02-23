using FemVed.Application.Interfaces;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAllOrders;

/// <summary>Handles <see cref="GetAllOrdersQuery"/>. Returns all orders across all users.</summary>
public sealed class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, List<OrderDto>>
{
    private readonly IRepository<Order> _orders;
    private readonly ILogger<GetAllOrdersQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllOrdersQueryHandler(IRepository<Order> orders, ILogger<GetAllOrdersQueryHandler> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    /// <summary>Returns all orders ordered by creation date descending.</summary>
    public async Task<List<OrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllOrders: loading all orders");

        var orders = await _orders.GetAllAsync(cancellationToken: cancellationToken);

        var result = orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderDto(
                OrderId:        o.Id,
                UserId:         o.UserId,
                DurationId:     o.DurationId,
                AmountPaid:     o.AmountPaid,
                CurrencyCode:   o.CurrencyCode,
                LocationCode:   o.LocationCode,
                DiscountAmount: o.DiscountAmount,
                Status:         o.Status.ToString(),
                Gateway:        o.PaymentGateway.ToString(),
                GatewayOrderId: o.GatewayOrderId,
                FailureReason:  o.FailureReason,
                CreatedAt:      o.CreatedAt))
            .ToList();

        _logger.LogInformation("GetAllOrders: returned {Count} orders", result.Count);
        return result;
    }
}
