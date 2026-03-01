using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Application.Payments.Events;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Commands.ProcessCashfreeWebhook;

/// <summary>
/// Handles <see cref="ProcessCashfreeWebhookCommand"/>.
/// Verifies the webhook signature, parses the event, updates order status,
/// and publishes <see cref="OrderPaidEvent"/> on success.
/// </summary>
public sealed class ProcessCashfreeWebhookCommandHandler : IRequestHandler<ProcessCashfreeWebhookCommand>
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _uow;
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessCashfreeWebhookCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public ProcessCashfreeWebhookCommandHandler(
        IRepository<Order> orders,
        IRepository<ProgramDuration> durations,
        IRepository<Domain.Entities.Program> programs,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork uow,
        IPublisher publisher,
        ILogger<ProcessCashfreeWebhookCommandHandler> logger)
    {
        _orders = orders;
        _durations = durations;
        _programs = programs;
        _gatewayFactory = gatewayFactory;
        _uow = uow;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>Verifies the webhook and processes the CashFree payment event.</summary>
    /// <param name="request">The command carrying the raw payload and signature headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedException">Thrown when the signature verification fails.</exception>
    public async Task Handle(ProcessCashfreeWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing CashFree webhook");

        // ── 1. Verify signature ──────────────────────────────────────────────
        var gateway = _gatewayFactory.GetGatewayByType(PaymentGateway.CashFree);
        var headers = new Dictionary<string, string>
        {
            ["x-webhook-signature"] = request.Signature,
            ["x-webhook-timestamp"] = request.Timestamp
        };

        var isValid = await gateway.VerifyWebhookSignatureAsync(
            request.RawPayload, headers, cancellationToken);

        if (!isValid)
        {
            _logger.LogWarning("CashFree webhook signature verification failed");
            throw new UnauthorizedException("Invalid CashFree webhook signature.");
        }

        // ── 2. Parse event ───────────────────────────────────────────────────
        using var doc = JsonDocument.Parse(request.RawPayload);
        var root = doc.RootElement;

        var eventType = root.GetProperty("type").GetString() ?? string.Empty;
        var orderNode = root.GetProperty("data").GetProperty("order");
        var internalOrderId = orderNode.GetProperty("order_id").GetString() ?? string.Empty;

        if (!Guid.TryParse(internalOrderId, out var orderId))
        {
            _logger.LogWarning("CashFree webhook has non-GUID order_id '{OrderId}' — ignored", internalOrderId);
            return;
        }

        // ── 3. Load order ────────────────────────────────────────────────────
        var order = await _orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("CashFree webhook: order {OrderId} not found — ignored", orderId);
            return;
        }

        // ── 4. Update order status ───────────────────────────────────────────
        if (eventType == "PAYMENT_SUCCESS_WEBHOOK")
        {
            if (order.Status == OrderStatus.Paid)
            {
                _logger.LogInformation("Order {OrderId} already marked Paid — skipping duplicate webhook", orderId);
                return;
            }

            var paymentNode = root.GetProperty("data").GetProperty("payment");
            order.GatewayPaymentId = paymentNode.TryGetProperty("cf_payment_id", out var cfId)
                ? cfId.ToString()
                : null;
            order.GatewayResponse = request.RawPayload;
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            _orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as Paid (CashFree)", orderId);

            // ── 5. Resolve ProgramId + ExpertId and publish domain event ────
            await PublishOrderPaidEventAsync(order, cancellationToken);
        }
        else if (eventType is "PAYMENT_FAILED_WEBHOOK" or "PAYMENT_USER_DROPPED_WEBHOOK")
        {
            order.Status = OrderStatus.Failed;
            order.FailureReason = eventType;
            order.GatewayResponse = request.RawPayload;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            _orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as Failed — event: {EventType}", orderId, eventType);

            await _publisher.Publish(new OrderFailedEvent(order.Id, order.UserId), cancellationToken);
        }
        else
        {
            _logger.LogInformation("CashFree webhook event '{EventType}' for order {OrderId} — no action taken",
                eventType, orderId);
        }
    }

    private async Task PublishOrderPaidEventAsync(Order order, CancellationToken cancellationToken)
    {
        var duration = await _durations.FirstOrDefaultAsync(
            d => d.Id == order.DurationId, cancellationToken);

        if (duration is null)
        {
            _logger.LogError("OrderPaidEvent: ProgramDuration {DurationId} not found for order {OrderId}",
                order.DurationId, order.Id);
            return;
        }

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == duration.ProgramId, cancellationToken);

        if (program is null)
        {
            _logger.LogError("OrderPaidEvent: Program {ProgramId} not found for order {OrderId}",
                duration.ProgramId, order.Id);
            return;
        }

        await _publisher.Publish(new OrderPaidEvent(
            OrderId: order.Id,
            UserId: order.UserId,
            ProgramId: program.Id,
            DurationId: order.DurationId,
            ExpertId: program.ExpertId), cancellationToken);
    }
}
