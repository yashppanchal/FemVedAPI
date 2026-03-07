using FemVed.Application.Interfaces;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Queries.GetMyRefunds;

/// <summary>
/// Handles <see cref="GetMyRefundsQuery"/>.
/// Loads all orders for the user, then returns all refunds against those orders, newest first.
/// </summary>
public sealed class GetMyRefundsQueryHandler : IRequestHandler<GetMyRefundsQuery, List<RefundDto>>
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<Refund> _refunds;
    private readonly ILogger<GetMyRefundsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetMyRefundsQueryHandler(
        IRepository<Order> orders,
        IRepository<Refund> refunds,
        ILogger<GetMyRefundsQueryHandler> logger)
    {
        _orders  = orders;
        _refunds = refunds;
        _logger  = logger;
    }

    /// <summary>Returns all refund records for orders belonging to the authenticated user, newest first.</summary>
    /// <param name="request">Query containing the authenticated user's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of <see cref="RefundDto"/> ordered by creation date descending.</returns>
    public async Task<List<RefundDto>> Handle(GetMyRefundsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyRefunds: loading refunds for user {UserId}", request.UserId);

        var userOrders = await _orders.GetAllAsync(
            o => o.UserId == request.UserId, cancellationToken);

        if (userOrders.Count == 0)
        {
            _logger.LogInformation("GetMyRefunds: user {UserId} has no orders", request.UserId);
            return [];
        }

        var orderIds = userOrders.Select(o => o.Id).ToHashSet();
        var currencyByOrderId = userOrders.ToDictionary(o => o.Id, o => o.CurrencyCode);

        var refunds = await _refunds.GetAllAsync(
            r => orderIds.Contains(r.OrderId), cancellationToken);

        var result = refunds
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RefundDto(
                RefundId:        r.Id,
                OrderId:         r.OrderId,
                RefundAmount:    r.RefundAmount,
                CurrencyCode:    currencyByOrderId.TryGetValue(r.OrderId, out var cc) ? cc : string.Empty,
                Reason:          r.Reason,
                Status:          r.Status.ToString(),
                GatewayRefundId: r.GatewayRefundId,
                CreatedAt:       r.CreatedAt))
            .ToList();

        _logger.LogInformation("GetMyRefunds: returning {Count} refund(s) for user {UserId}",
            result.Count, request.UserId);

        return result;
    }
}
