using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Commands.InitiateRefund;

/// <summary>
/// Handles <see cref="InitiateRefundCommand"/>.
/// Validates the order is eligible for refund, calls the payment gateway,
/// and persists a <see cref="Refund"/> record with the gateway's response.
/// </summary>
public sealed class InitiateRefundCommandHandler : IRequestHandler<InitiateRefundCommand>
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<Refund> _refunds;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InitiateRefundCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public InitiateRefundCommandHandler(
        IRepository<Order> orders,
        IRepository<Refund> refunds,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork uow,
        ILogger<InitiateRefundCommandHandler> logger)
    {
        _orders = orders;
        _refunds = refunds;
        _gatewayFactory = gatewayFactory;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Initiates the refund and persists the result.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the order is not found.</exception>
    /// <exception cref="DomainException">Thrown when the order is not paid or the refund amount exceeds what was charged.</exception>
    public async Task Handle(InitiateRefundCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initiating refund for order {OrderId}, amount {Amount}",
            request.OrderId, request.RefundAmount);

        // ── 1. Load and validate order ───────────────────────────────────────
        var order = await _orders.FirstOrDefaultAsync(
            o => o.Id == request.OrderId,
            cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != OrderStatus.Paid)
            throw new DomainException(
                $"Only PAID orders can be refunded. Current status: {order.Status}.");

        if (request.RefundAmount > order.AmountPaid)
            throw new DomainException(
                $"Refund amount {request.RefundAmount} exceeds the original charge of {order.AmountPaid}.");

        // ── 2. Create pending refund record ──────────────────────────────────
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            RefundAmount = request.RefundAmount,
            Reason = request.Reason,
            Status = RefundStatus.Pending,
            InitiatedBy = request.InitiatedByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _refunds.AddAsync(refund);
        await _uow.SaveChangesAsync(cancellationToken);

        // ── 3. Call gateway ──────────────────────────────────────────────────
        var gateway = _gatewayFactory.GetGatewayByType(order.PaymentGateway);

        var gatewayRequest = new GatewayRefundRequest(
            GatewayOrderId: order.GatewayOrderId ?? string.Empty,
            GatewayPaymentId: order.GatewayPaymentId,
            InternalRefundId: refund.Id.ToString(),
            Amount: request.RefundAmount,
            CurrencyCode: order.CurrencyCode,
            Reason: request.Reason);

        var result = await gateway.RefundAsync(gatewayRequest, cancellationToken);

        // ── 4. Update refund and order status ────────────────────────────────
        refund.GatewayRefundId = result.GatewayRefundId;
        refund.Status = result.Success ? RefundStatus.Completed : RefundStatus.Failed;
        refund.UpdatedAt = DateTimeOffset.UtcNow;
        _refunds.Update(refund);

        if (result.Success)
        {
            order.Status = OrderStatus.Refunded;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            _orders.Update(order);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        if (result.Success)
            _logger.LogInformation("Refund {RefundId} completed for order {OrderId}", refund.Id, order.Id);
        else
            _logger.LogError("Refund {RefundId} failed for order {OrderId}: {Reason}",
                refund.Id, order.Id, result.FailureReason);

        if (!result.Success)
            throw new DomainException($"Gateway refund failed: {result.FailureReason}");
    }
}
