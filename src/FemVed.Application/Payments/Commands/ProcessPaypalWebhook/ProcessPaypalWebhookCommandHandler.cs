using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Application.Payments.Events;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Commands.ProcessPaypalWebhook;

/// <summary>
/// Handles <see cref="ProcessPaypalWebhookCommand"/>.
/// Verifies the webhook via PayPal's API, parses the event, updates order status,
/// and publishes <see cref="OrderPaidEvent"/> on a successful capture or
/// <see cref="OrderFailedEvent"/> on a denied capture.
/// </summary>
public sealed class ProcessPaypalWebhookCommandHandler : IRequestHandler<ProcessPaypalWebhookCommand>
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _uow;
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessPaypalWebhookCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public ProcessPaypalWebhookCommandHandler(
        IRepository<Order> orders,
        IRepository<ProgramDuration> durations,
        IRepository<Domain.Entities.Program> programs,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork uow,
        IPublisher publisher,
        ILogger<ProcessPaypalWebhookCommandHandler> logger)
    {
        _orders        = orders;
        _durations     = durations;
        _programs      = programs;
        _gatewayFactory = gatewayFactory;
        _uow           = uow;
        _publisher     = publisher;
        _logger        = logger;
    }

    /// <summary>Verifies the webhook and processes the PayPal payment event.</summary>
    /// <param name="request">The command carrying the raw payload and verification headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedException">Thrown when PayPal signature verification fails.</exception>
    public async Task Handle(ProcessPaypalWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing PayPal webhook");

        // ── 1. Verify signature via PayPal API ───────────────────────────────
        var gateway = _gatewayFactory.GetGatewayByType(PaymentGateway.PayPal);
        var headers = new Dictionary<string, string>
        {
            ["paypal-auth-algo"]        = request.AuthAlgo,
            ["paypal-cert-url"]         = request.CertUrl,
            ["paypal-transmission-id"]  = request.TransmissionId,
            ["paypal-transmission-sig"] = request.TransmissionSig,
            ["paypal-transmission-time"]= request.TransmissionTime
        };

        var isValid = await gateway.VerifyWebhookSignatureAsync(
            request.RawPayload, headers, cancellationToken);

        if (!isValid)
        {
            _logger.LogWarning("PayPal webhook signature verification failed");
            throw new UnauthorizedException("Invalid PayPal webhook signature.");
        }

        // ── 2. Parse event ───────────────────────────────────────────────────
        using var doc = JsonDocument.Parse(request.RawPayload);
        var root      = doc.RootElement;
        var eventType = root.GetProperty("event_type").GetString() ?? string.Empty;

        if (eventType == "PAYMENT.CAPTURE.COMPLETED")
        {
            // ── 3a. Success path ─────────────────────────────────────────────
            // custom_id holds our internal Order UUID (set when creating the PayPal order)
            var resource        = root.GetProperty("resource");
            var internalOrderId = resource.TryGetProperty("custom_id", out var cid) ? cid.GetString() : null;
            var captureId       = resource.TryGetProperty("id", out var capId) ? capId.GetString() : null;

            if (!Guid.TryParse(internalOrderId, out var orderId))
            {
                _logger.LogWarning("PayPal webhook: non-GUID custom_id '{Id}' — ignored", internalOrderId);
                return;
            }

            var order = await _orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("PayPal webhook: order {OrderId} not found — ignored", orderId);
                return;
            }

            if (order.Status == OrderStatus.Paid)
            {
                _logger.LogInformation("Order {OrderId} already marked Paid — skipping duplicate webhook", orderId);
                return;
            }

            order.GatewayPaymentId = captureId;
            order.GatewayResponse  = request.RawPayload;
            order.Status           = OrderStatus.Paid;
            order.UpdatedAt        = DateTimeOffset.UtcNow;
            _orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as Paid (PayPal)", orderId);

            await PublishOrderPaidEventAsync(order, cancellationToken);
        }
        else if (eventType == "PAYMENT.CAPTURE.DENIED")
        {
            // ── 3b. Failure path ─────────────────────────────────────────────
            var resource        = root.GetProperty("resource");
            var internalOrderId = resource.TryGetProperty("custom_id", out var cid) ? cid.GetString() : null;

            if (!Guid.TryParse(internalOrderId, out var orderId))
            {
                _logger.LogWarning("PayPal webhook (DENIED): non-GUID custom_id '{Id}' — ignored", internalOrderId);
                return;
            }

            var order = await _orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("PayPal webhook (DENIED): order {OrderId} not found — ignored", orderId);
                return;
            }

            if (order.Status == OrderStatus.Failed)
            {
                _logger.LogInformation("Order {OrderId} already marked Failed — skipping duplicate webhook", orderId);
                return;
            }

            order.Status          = OrderStatus.Failed;
            order.FailureReason   = eventType;
            order.GatewayResponse = request.RawPayload;
            order.UpdatedAt       = DateTimeOffset.UtcNow;
            _orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} marked as Failed (PayPal DENIED)", orderId);

            await _publisher.Publish(new OrderFailedEvent(order.Id, order.UserId), cancellationToken);
        }
        else
        {
            _logger.LogInformation("PayPal event '{EventType}' — no action taken", eventType);
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
            OrderId:    order.Id,
            UserId:     order.UserId,
            ProgramId:  program.Id,
            DurationId: order.DurationId,
            ExpertId:   program.ExpertId), cancellationToken);
    }
}
