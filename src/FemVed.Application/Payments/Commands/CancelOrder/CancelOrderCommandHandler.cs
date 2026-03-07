using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Commands.CancelOrder;

/// <summary>
/// Handles <see cref="CancelOrderCommand"/>.
/// Verifies ownership, checks the order is still Pending, then sets status to Cancelled.
/// </summary>
public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IRepository<Order> _orders;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public CancelOrderCommandHandler(
        IRepository<Order> orders,
        IUnitOfWork uow,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _orders = orders;
        _uow    = uow;
        _logger = logger;
    }

    /// <summary>Cancels the order after verifying ownership and status.</summary>
    /// <param name="request">The cancel command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the order does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the order does not belong to the requesting user.</exception>
    /// <exception cref="DomainException">Thrown when the order is not in Pending status.</exception>
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CancelOrder: user {UserId} cancelling order {OrderId}",
            request.UserId, request.OrderId);

        var order = await _orders.FirstOrDefaultAsync(
            o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.UserId != request.UserId)
            throw new ForbiddenException("You can only cancel your own orders.");

        if (order.Status != OrderStatus.Pending)
            throw new DomainException(
                $"Only pending orders can be cancelled. This order is {order.Status}.");

        order.Status    = OrderStatus.Cancelled;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CancelOrder: order {OrderId} cancelled successfully", request.OrderId);
    }
}
