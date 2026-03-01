using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Application.Payments.Events;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FemVed.Application.Payments.Commands.ProcessPaypalWebhook;

/// <summary>
/// Handles <see cref="ProcessPaypalWebhookCommand"/>.
/// Verifies the webhook via PayPal's API, parses the event, updates order status,
/// and publishes <see cref="OrderPaidEvent"/> on a successful capture or
/// <see cref="OrderFailedEvent"/> on a denied capture.
/// Also handles <c>PAYMENT.CAPTURE.REFUNDED</c> (including manual dashboard refunds)
/// and logs <c>CUSTOMER.DISPUTE.CREATED</c> for admin attention.
/// </summary>
public sealed class ProcessPaypalWebhookCommandHandler : IRequestHandler<ProcessPaypalWebhookCommand>
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<Refund> _refunds;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _uow;
    private readonly IPublisher _publisher;
    private readonly ILogger<ProcessPaypalWebhookCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public ProcessPaypalWebhookCommandHandler(
        IRepository<Order> orders,
        IRepository<Refund> refunds,
        IRepository<ProgramDuration> durations,
        IRepository<Domain.Entities.Program> programs,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork uow,
        IPublisher publisher,
        ILogger<ProcessPaypalWebhookCommandHandler> logger)
    {
        _orders         = orders;
        _refunds        = refunds;
        _durations      = durations;
        _programs       = programs;
        _gatewayFactory = gatewayFactory;
        _uow            = uow;
        _publisher      = publisher;
        _logger         = logger;
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

        if (eventType == "CHECKOUT.ORDER.APPROVED")
        {
            // ── 3c. Capture path ─────────────────────────────────────────────
            // Buyer approved the order on PayPal's checkout page.
            // Call capture immediately — PayPal then fires PAYMENT.CAPTURE.COMPLETED
            // which marks the order as Paid in the next webhook delivery.
            var resource      = root.GetProperty("resource");
            var paypalOrderId = resource.TryGetProperty("id", out var pid) ? pid.GetString() : null;

            if (string.IsNullOrEmpty(paypalOrderId))
            {
                _logger.LogWarning("CHECKOUT.ORDER.APPROVED: missing resource.id — ignored");
                return;
            }

            _logger.LogInformation("CHECKOUT.ORDER.APPROVED: capturing PayPal order {PayPalOrderId}", paypalOrderId);
            var captureId = await gateway.CaptureOrderAsync(paypalOrderId, cancellationToken);

            if (captureId is null)
                _logger.LogError("CHECKOUT.ORDER.APPROVED: capture failed for PayPal order {PayPalOrderId}", paypalOrderId);
            else
                _logger.LogInformation("CHECKOUT.ORDER.APPROVED: capture succeeded, captureId={CaptureId} — awaiting PAYMENT.CAPTURE.COMPLETED", captureId);
        }
        else if (eventType == "PAYMENT.CAPTURE.COMPLETED" || eventType == "CHECKOUT.ORDER.COMPLETED")
        {
            // ── 3a. Success path ─────────────────────────────────────────────
            // PAYMENT.CAPTURE.COMPLETED: resource IS the capture object.
            //   resource.custom_id  = our internal Order UUID
            //   resource.id         = capture ID
            //
            // CHECKOUT.ORDER.COMPLETED: resource IS the order object.
            //   resource.purchase_units[0].custom_id                          = our internal Order UUID
            //   resource.purchase_units[0].payments.captures[0].id            = capture ID
            var resource = root.GetProperty("resource");

            string? internalOrderId;
            string? captureId;

            if (eventType == "PAYMENT.CAPTURE.COMPLETED")
            {
                internalOrderId = resource.TryGetProperty("custom_id", out var cid) ? cid.GetString() : null;
                captureId       = resource.TryGetProperty("id", out var capId) ? capId.GetString() : null;
            }
            else // CHECKOUT.ORDER.COMPLETED
            {
                internalOrderId = null;
                captureId       = null;
                if (resource.TryGetProperty("purchase_units", out var units))
                {
                    foreach (var unit in units.EnumerateArray())
                    {
                        internalOrderId ??= unit.TryGetProperty("custom_id", out var cid2) ? cid2.GetString()
                            : unit.TryGetProperty("reference_id", out var rid) ? rid.GetString() : null;

                        if (unit.TryGetProperty("payments", out var payments)
                            && payments.TryGetProperty("captures", out var captures))
                        {
                            foreach (var cap in captures.EnumerateArray())
                            {
                                captureId ??= cap.TryGetProperty("id", out var capId2) ? capId2.GetString() : null;
                                break;
                            }
                        }
                        break;
                    }
                }
            }

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
        else if (eventType == "PAYMENT.CAPTURE.REFUNDED")
        {
            // ── 3d. Refund path ──────────────────────────────────────────────
            // PayPal fires this when any capture is refunded — either via our
            // /orders/{id}/refund endpoint (synchronous, already recorded) OR
            // when an admin manually issues the refund from the PayPal dashboard.
            //
            // resource.id        = PayPal refund ID
            // resource.links[]   = contains rel="up" href pointing to the capture
            //   e.g. ".../v2/payments/captures/{captureId}"
            await HandleCaptureRefundedAsync(root, request.RawPayload, cancellationToken);
        }
        else if (eventType == "CUSTOMER.DISPUTE.CREATED")
        {
            // ── 3e. Dispute / chargeback alert ───────────────────────────────
            // A buyer has opened a dispute. We must respond on PayPal's Resolution
            // Centre within 10 days or PayPal auto-grants the dispute.
            // Action required: admin must log in to PayPal and respond manually.
            var resource      = root.GetProperty("resource");
            var disputeId     = resource.TryGetProperty("dispute_id", out var did)  ? did.GetString()  : "unknown";
            var reason        = resource.TryGetProperty("reason",     out var drn)  ? drn.GetString()  : "unknown";
            var disputeAmount = resource.TryGetProperty("dispute_amount", out var da)
                && da.TryGetProperty("value", out var dv) ? dv.GetString() : "unknown";

            _logger.LogWarning(
                "ACTION REQUIRED — PayPal dispute created. DisputeId={DisputeId}, Reason={Reason}, " +
                "Amount={Amount}. Log in to PayPal Resolution Centre and respond within 10 days.",
                disputeId, reason, disputeAmount);
        }
        else
        {
            _logger.LogInformation("PayPal event '{EventType}' — no action taken", eventType);
        }
    }

    /// <summary>
    /// Handles <c>PAYMENT.CAPTURE.REFUNDED</c>.
    /// Idempotent — skips if the refund is already recorded.
    /// Creates a new <see cref="Refund"/> row for manual dashboard refunds and marks
    /// the linked <see cref="Order"/> as <see cref="OrderStatus.Refunded"/>.
    /// </summary>
    private async Task HandleCaptureRefundedAsync(
        JsonElement root,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        var resource       = root.GetProperty("resource");
        var paypalRefundId = resource.TryGetProperty("id", out var rid) ? rid.GetString() : null;

        if (string.IsNullOrEmpty(paypalRefundId))
        {
            _logger.LogWarning("PAYMENT.CAPTURE.REFUNDED: missing resource.id — ignored");
            return;
        }

        // ── Idempotency: if we already have a Refund row for this PayPal refund ID,
        //    it was created synchronously by InitiateRefundCommandHandler — nothing to do.
        var existing = await _refunds.FirstOrDefaultAsync(
            r => r.GatewayRefundId == paypalRefundId, cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation(
                "PAYMENT.CAPTURE.REFUNDED: refund {PayPalRefundId} already recorded as {RefundId} — skipping",
                paypalRefundId, existing.Id);
            return;
        }

        // ── Manual dashboard refund — locate Order via capture ID from links ──
        // PayPal embeds a link with rel="up" that points to
        //   /v2/payments/captures/{captureId}
        string? captureId = null;
        if (resource.TryGetProperty("links", out var links))
        {
            foreach (var link in links.EnumerateArray())
            {
                if (link.TryGetProperty("rel", out var rel) && rel.GetString() == "up"
                    && link.TryGetProperty("href", out var href))
                {
                    var match = Regex.Match(href.GetString() ?? string.Empty,
                        @"/captures/([^/]+)$");
                    if (match.Success)
                        captureId = match.Groups[1].Value;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(captureId))
        {
            _logger.LogWarning(
                "PAYMENT.CAPTURE.REFUNDED: cannot determine capture ID from links for refund {PayPalRefundId} — ignored",
                paypalRefundId);
            return;
        }

        var order = await _orders.FirstOrDefaultAsync(
            o => o.GatewayPaymentId == captureId, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning(
                "PAYMENT.CAPTURE.REFUNDED: no order found for captureId={CaptureId} — ignored",
                captureId);
            return;
        }

        // ── Parse refunded amount ─────────────────────────────────────────────
        decimal refundedAmount = 0m;
        if (resource.TryGetProperty("amount", out var amountEl)
            && amountEl.TryGetProperty("value", out var amountVal))
        {
            decimal.TryParse(amountVal.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out refundedAmount);
        }

        // ── Create refund record for the manual refund ────────────────────────
        var refund = new Refund
        {
            Id              = Guid.NewGuid(),
            OrderId         = order.Id,
            RefundAmount    = refundedAmount,
            Reason          = "Manual refund issued via PayPal dashboard",
            GatewayRefundId = paypalRefundId,
            Status          = RefundStatus.Completed,
            InitiatedBy     = Guid.Empty,   // No internal admin — external action
            CreatedAt       = DateTimeOffset.UtcNow,
            UpdatedAt       = DateTimeOffset.UtcNow
        };

        await _refunds.AddAsync(refund);

        order.Status    = OrderStatus.Refunded;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        _orders.Update(order);

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "PAYMENT.CAPTURE.REFUNDED: manual PayPal dashboard refund detected. " +
            "OrderId={OrderId}, PayPalRefundId={PayPalRefundId}, Amount={Amount}. " +
            "RefundRecord={RefundId} created. Order marked Refunded.",
            order.Id, paypalRefundId, refundedAmount, refund.Id);
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
