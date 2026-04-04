using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Application.Payments.Events;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Commands.ProcessStripeWebhook;

/// <summary>
/// Handles <see cref="ProcessStripeWebhookCommand"/>.
/// Verifies the Stripe webhook signature, parses the event type, and dispatches
/// the appropriate domain event or updates order/refund state.
///
/// Handled Stripe events:
/// <list type="bullet">
///   <item><c>checkout.session.completed</c> — payment succeeded → <see cref="OrderPaidEvent"/>.</item>
///   <item><c>checkout.session.expired</c> — user abandoned checkout → <see cref="OrderFailedEvent"/>.</item>
///   <item><c>payment_intent.payment_failed</c> — payment declined → <see cref="OrderFailedEvent"/>.</item>
///   <item><c>charge.refunded</c> — full or partial refund processed (including dashboard refunds).</item>
///   <item><c>charge.dispute.created</c> — chargeback opened → logged as Warning for admin action.</item>
/// </list>
/// Signature failures → <see cref="UnauthorizedException"/>. All other unrecognised events → 200 OK (no-op).
/// </summary>
public sealed class ProcessStripeWebhookCommandHandler : IRequestHandler<ProcessStripeWebhookCommand>
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<Refund> _refunds;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _uow;
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessStripeWebhookCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public ProcessStripeWebhookCommandHandler(
        IRepository<Order> orders,
        IRepository<Refund> refunds,
        IRepository<ProgramDuration> durations,
        IRepository<Domain.Entities.Program> programs,
        IRepository<LibraryVideo> videos,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork uow,
        IPublisher publisher,
        ILogger<ProcessStripeWebhookCommandHandler> logger)
    {
        _orders         = orders;
        _refunds        = refunds;
        _durations      = durations;
        _programs       = programs;
        _videos         = videos;
        _gatewayFactory = gatewayFactory;
        _uow            = uow;
        _publisher      = publisher;
        _logger         = logger;
    }

    /// <summary>Verifies the webhook and processes the Stripe event.</summary>
    /// <param name="request">The command carrying the raw payload and signature header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="UnauthorizedException">Thrown when Stripe signature verification fails.</exception>
    public async Task Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing Stripe webhook");

        // ── 1. Verify signature ──────────────────────────────────────────────
        var gateway = _gatewayFactory.GetGatewayByType(PaymentGateway.Stripe);
        var headers = new Dictionary<string, string>
        {
            ["stripe-signature"] = request.StripeSignature
        };

        var isValid = await gateway.VerifyWebhookSignatureAsync(
            request.RawPayload, headers, cancellationToken);

        if (!isValid)
        {
            _logger.LogWarning("Stripe webhook signature verification failed");
            throw new UnauthorizedException("Invalid Stripe webhook signature.");
        }

        // ── 2. Parse event ───────────────────────────────────────────────────
        using var doc       = JsonDocument.Parse(request.RawPayload);
        var root            = doc.RootElement;
        var eventType       = root.GetProperty("type").GetString() ?? string.Empty;
        var dataObject      = root.GetProperty("data").GetProperty("object");

        _logger.LogInformation("Stripe event '{EventType}' received", eventType);

        switch (eventType)
        {
            case "checkout.session.completed":
                await HandleSessionCompletedAsync(dataObject, request.RawPayload, cancellationToken);
                break;

            case "checkout.session.expired":
                await HandleSessionExpiredAsync(dataObject, cancellationToken);
                break;

            case "payment_intent.payment_failed":
                await HandlePaymentIntentFailedAsync(dataObject, cancellationToken);
                break;

            case "charge.refunded":
                await HandleChargeRefundedAsync(dataObject, request.RawPayload, cancellationToken);
                break;

            case "charge.dispute.created":
                HandleDisputeCreated(dataObject);
                break;

            default:
                _logger.LogInformation("Stripe event '{EventType}' — no action taken", eventType);
                break;
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    /// <summary>
    /// Handles <c>checkout.session.completed</c>.
    /// Marks the order as Paid and publishes <see cref="OrderPaidEvent"/>.
    /// Idempotent — skips if the order is already Paid.
    /// </summary>
    private async Task HandleSessionCompletedAsync(
        JsonElement session,
        string rawPayload,
        CancellationToken ct)
    {
        // client_reference_id = our internal Order UUID (set at checkout session creation)
        var internalOrderId = session.TryGetProperty("client_reference_id", out var cid) ? cid.GetString() : null;

        if (!Guid.TryParse(internalOrderId, out var orderId))
        {
            _logger.LogWarning("Stripe checkout.session.completed: non-GUID client_reference_id '{Id}' — ignored", internalOrderId);
            return;
        }

        var order = await _orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Stripe checkout.session.completed: order {OrderId} not found — ignored", orderId);
            return;
        }

        if (order.Status == OrderStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} already marked Paid — skipping duplicate Stripe webhook", orderId);
            return;
        }

        // payment_intent = Stripe PaymentIntent ID — store as GatewayPaymentId for refunds
        var paymentIntentId = session.TryGetProperty("payment_intent", out var pi) ? pi.GetString() : null;

        order.GatewayPaymentId = paymentIntentId;
        order.GatewayResponse  = rawPayload;
        order.Status           = OrderStatus.Paid;
        order.UpdatedAt        = DateTimeOffset.UtcNow;
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} marked as Paid (Stripe)", orderId);

        await PublishOrderPaidEventAsync(order, ct);
    }

    /// <summary>
    /// Handles <c>checkout.session.expired</c>.
    /// User abandoned the checkout page — marks order as Failed.
    /// </summary>
    private async Task HandleSessionExpiredAsync(JsonElement session, CancellationToken ct)
    {
        var internalOrderId = session.TryGetProperty("client_reference_id", out var cid) ? cid.GetString() : null;

        if (!Guid.TryParse(internalOrderId, out var orderId))
        {
            _logger.LogWarning("Stripe checkout.session.expired: non-GUID client_reference_id '{Id}' — ignored", internalOrderId);
            return;
        }

        var order = await _orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Stripe checkout.session.expired: order {OrderId} not found — ignored", orderId);
            return;
        }

        if (order.Status == OrderStatus.Failed || order.Status == OrderStatus.Cancelled)
        {
            _logger.LogInformation("Order {OrderId} already terminal — skipping session expired event", orderId);
            return;
        }

        order.Status        = OrderStatus.Failed;
        order.FailureReason = "checkout.session.expired — user did not complete payment";
        order.UpdatedAt     = DateTimeOffset.UtcNow;
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} marked as Failed (Stripe session expired)", orderId);

        await _publisher.Publish(new OrderFailedEvent(order.Id, order.UserId), ct);
    }

    /// <summary>
    /// Handles <c>payment_intent.payment_failed</c>.
    /// Uses <c>metadata.internal_order_id</c> (set at session creation) to locate the order.
    /// </summary>
    private async Task HandlePaymentIntentFailedAsync(JsonElement paymentIntent, CancellationToken ct)
    {
        // internal_order_id was stored in payment_intent_data[metadata] at session creation
        string? internalOrderId = null;
        if (paymentIntent.TryGetProperty("metadata", out var meta)
            && meta.TryGetProperty("internal_order_id", out var idProp))
        {
            internalOrderId = idProp.GetString();
        }

        if (!Guid.TryParse(internalOrderId, out var orderId))
        {
            _logger.LogWarning("Stripe payment_intent.payment_failed: non-GUID metadata.internal_order_id '{Id}' — ignored", internalOrderId);
            return;
        }

        var order = await _orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Stripe payment_intent.payment_failed: order {OrderId} not found — ignored", orderId);
            return;
        }

        // Skip if already in any terminal state — Paid means the payment succeeded before this failure event arrived
        if (order.Status is OrderStatus.Failed or OrderStatus.Paid or OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            _logger.LogInformation("Order {OrderId} already in terminal state {Status} — skipping payment_intent.payment_failed", orderId, order.Status);
            return;
        }

        var failureMessage = "Payment declined.";
        if (paymentIntent.TryGetProperty("last_payment_error", out var lpe)
            && lpe.TryGetProperty("message", out var msg))
        {
            failureMessage = msg.GetString() ?? failureMessage;
        }

        order.Status        = OrderStatus.Failed;
        order.FailureReason = failureMessage;
        order.UpdatedAt     = DateTimeOffset.UtcNow;
        _orders.Update(order);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} marked as Failed (Stripe payment_intent failed: {Reason})", orderId, failureMessage);

        await _publisher.Publish(new OrderFailedEvent(order.Id, order.UserId), ct);
    }

    /// <summary>
    /// Handles <c>charge.refunded</c>.
    /// Idempotent — skips if the refund is already recorded (e.g. from our own InitiateRefund endpoint).
    /// Creates a <see cref="Refund"/> row for manual dashboard refunds and marks the <see cref="Order"/> as Refunded.
    /// </summary>
    private async Task HandleChargeRefundedAsync(JsonElement charge, string rawPayload, CancellationToken ct)
    {
        var paymentIntentId = charge.TryGetProperty("payment_intent", out var pi) ? pi.GetString() : null;

        if (string.IsNullOrEmpty(paymentIntentId))
        {
            _logger.LogWarning("Stripe charge.refunded: missing payment_intent — ignored");
            return;
        }

        // Locate the order via GatewayPaymentId (= PaymentIntent ID stored on checkout.session.completed)
        var order = await _orders.FirstOrDefaultAsync(o => o.GatewayPaymentId == paymentIntentId, ct);
        if (order is null)
        {
            _logger.LogWarning("Stripe charge.refunded: no order found for PaymentIntent {PaymentIntentId} — ignored", paymentIntentId);
            return;
        }

        // Process every refund in refunds.data (handles multiple partial refunds on the same charge).
        // Each Stripe refund has a unique ID — idempotency is enforced per refund ID.
        if (!charge.TryGetProperty("refunds", out var refunds)
            || !refunds.TryGetProperty("data", out var refundData))
        {
            _logger.LogWarning("Stripe charge.refunded: missing refunds.data — ignored");
            return;
        }

        var anyNewRefund = false;
        foreach (var refundItem in refundData.EnumerateArray())
        {
            var stripeRefundId = refundItem.TryGetProperty("id", out var rid) ? rid.GetString() : null;
            if (string.IsNullOrEmpty(stripeRefundId)) continue;

            // Idempotency: already recorded by InitiateRefundCommandHandler or a prior webhook delivery?
            var existing = await _refunds.FirstOrDefaultAsync(r => r.GatewayRefundId == stripeRefundId, ct);
            if (existing is not null)
            {
                _logger.LogInformation("Stripe charge.refunded: refund {StripeRefundId} already recorded — skipping", stripeRefundId);
                continue;
            }

            decimal refundedAmount = 0m;
            if (refundItem.TryGetProperty("amount", out var amt))
                refundedAmount = amt.GetInt64() / 100m; // convert from smallest unit

            var refund = new Refund
            {
                Id              = Guid.NewGuid(),
                OrderId         = order.Id,
                RefundAmount    = refundedAmount,
                Reason          = "Manual refund issued via Stripe dashboard",
                GatewayRefundId = stripeRefundId,
                Status          = RefundStatus.Completed,
                InitiatedBy     = Guid.Empty, // No internal admin — external action
                CreatedAt       = DateTimeOffset.UtcNow,
                UpdatedAt       = DateTimeOffset.UtcNow
            };

            await _refunds.AddAsync(refund);
            anyNewRefund = true;

            _logger.LogWarning(
                "Stripe charge.refunded: manual dashboard refund detected. OrderId={OrderId}, StripeRefundId={StripeRefundId}, Amount={Amount}.",
                order.Id, stripeRefundId, refundedAmount);
        }

        if (!anyNewRefund) return;

        order.Status    = OrderStatus.Refunded;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        _orders.Update(order);

        await _uow.SaveChangesAsync(ct);

        _logger.LogWarning("Stripe charge.refunded: order {OrderId} marked Refunded.", order.Id);
    }

    /// <summary>
    /// Handles <c>charge.dispute.created</c>.
    /// NOTE: <c>data.object</c> for this event is a <b>Dispute object</b> (not a Charge).
    /// Logs a warning — admin must respond in the Stripe Dashboard within the dispute window.
    /// </summary>
    private void HandleDisputeCreated(JsonElement dispute)
    {
        // Dispute object has "id" directly (not nested under "dispute")
        var disputeId = dispute.TryGetProperty("id", out var d) ? d.GetString() : "unknown";
        // Dispute object has "amount" (not "amount_disputed")
        var amount    = dispute.TryGetProperty("amount", out var a) ? (a.GetInt64() / 100m).ToString("F2") : "unknown";
        var reason    = dispute.TryGetProperty("reason", out var r) ? r.GetString() : "unknown";

        _logger.LogWarning(
            "ACTION REQUIRED — Stripe dispute created. DisputeId={DisputeId}, Amount={Amount}, Reason={Reason}. " +
            "Log in to Stripe Dashboard and respond within the dispute window.",
            disputeId, amount, reason);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task PublishOrderPaidEventAsync(Order order, CancellationToken ct)
    {
        if (order.OrderSource == OrderSource.Library && order.LibraryVideoId.HasValue)
        {
            var video = await _videos.FirstOrDefaultAsync(
                v => v.Id == order.LibraryVideoId.Value, ct);

            if (video is null)
            {
                _logger.LogError("Stripe OrderPaidEvent: LibraryVideo {VideoId} not found for order {OrderId}",
                    order.LibraryVideoId, order.Id);
                return;
            }

            await _publisher.Publish(new OrderPaidEvent(
                OrderId: order.Id,
                UserId: order.UserId,
                OrderSource: OrderSource.Library,
                ProgramId: null,
                DurationId: null,
                ExpertId: video.ExpertId,
                VideoId: video.Id), ct);
            return;
        }

        // Guided flow
        var duration = await _durations.FirstOrDefaultAsync(d => d.Id == order.DurationId, ct);
        if (duration is null)
        {
            _logger.LogError("Stripe OrderPaidEvent: ProgramDuration {DurationId} not found for order {OrderId}",
                order.DurationId, order.Id);
            return;
        }

        var program = await _programs.FirstOrDefaultAsync(p => p.Id == duration.ProgramId, ct);
        if (program is null)
        {
            _logger.LogError("Stripe OrderPaidEvent: Program {ProgramId} not found for order {OrderId}",
                duration.ProgramId, order.Id);
            return;
        }

        await _publisher.Publish(new OrderPaidEvent(
            OrderId:    order.Id,
            UserId:     order.UserId,
            OrderSource: OrderSource.Guided,
            ProgramId:  program.Id,
            DurationId: order.DurationId,
            ExpertId:   program.ExpertId), ct);
    }
}
