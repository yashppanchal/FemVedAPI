using FemVed.Application.Interfaces;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Commands.InitiateOrder;

/// <summary>
/// Handles <see cref="InitiateOrderCommand"/>.
/// Validates the duration, applies any coupon, selects the gateway, creates an internal
/// <see cref="Order"/> record, and calls the gateway to create the external session/approval URL.
/// </summary>
public sealed class InitiateOrderCommandHandler
    : IRequestHandler<InitiateOrderCommand, InitiateOrderResponse>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly IRepository<Coupon> _coupons;
    private readonly IRepository<Order> _orders;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InitiateOrderCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public InitiateOrderCommandHandler(
        IRepository<User> users,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        IRepository<Coupon> coupons,
        IRepository<Order> orders,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork uow,
        ILogger<InitiateOrderCommandHandler> logger)
    {
        _users = users;
        _durations = durations;
        _prices = prices;
        _coupons = coupons;
        _orders = orders;
        _gatewayFactory = gatewayFactory;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Processes the order initiation request.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Gateway-specific order tokens and metadata.</returns>
    /// <exception cref="NotFoundException">Thrown if user, duration, or price record is not found.</exception>
    /// <exception cref="DomainException">Thrown for invalid coupon or no price for user's location.</exception>
    public async Task<InitiateOrderResponse> Handle(
        InitiateOrderCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initiating order for user {UserId}, duration {DurationId}",
            request.UserId, request.DurationId);

        // ── 1. Idempotency check ─────────────────────────────────────────────
        var existing = await _orders.FirstOrDefaultAsync(
            o => o.IdempotencyKey == request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation("Idempotent order {OrderId} returned for key {Key}",
                existing.Id, request.IdempotencyKey);
            return BuildResponseFromExistingOrder(existing);
        }

        // ── 2. Load user ─────────────────────────────────────────────────────
        var user = await _users.FirstOrDefaultAsync(
            u => u.Id == request.UserId && !u.IsDeleted && u.IsActive,
            cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        var locationCode = user.CountryIsoCode ?? "GB";

        // ── 3. Load duration & price ─────────────────────────────────────────
        var duration = await _durations.FirstOrDefaultAsync(
            d => d.Id == request.DurationId && d.IsActive,
            cancellationToken)
            ?? throw new NotFoundException("ProgramDuration", request.DurationId);

        var price = await _prices.FirstOrDefaultAsync(
            dp => dp.DurationId == request.DurationId
               && dp.LocationCode == locationCode
               && dp.IsActive,
            cancellationToken)
            ?? await _prices.FirstOrDefaultAsync(
                dp => dp.DurationId == request.DurationId
                   && dp.LocationCode == "GB"
                   && dp.IsActive,
                cancellationToken)
            ?? throw new DomainException(
                $"No price is available for duration {request.DurationId} in location '{locationCode}'.");

        // ── 4. Apply coupon ──────────────────────────────────────────────────
        decimal discountAmount = 0;
        Coupon? coupon = null;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            coupon = await _coupons.FirstOrDefaultAsync(
                c => c.Code == request.CouponCode && c.IsActive,
                cancellationToken)
                ?? throw new DomainException($"Coupon '{request.CouponCode}' is invalid or does not exist.");

            var now = DateTimeOffset.UtcNow;
            if (coupon.ValidFrom.HasValue && now < coupon.ValidFrom)
                throw new DomainException($"Coupon '{request.CouponCode}' is not yet valid.");
            if (coupon.ValidUntil.HasValue && now > coupon.ValidUntil)
                throw new DomainException($"Coupon '{request.CouponCode}' has expired.");
            if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses)
                throw new DomainException($"Coupon '{request.CouponCode}' has reached its maximum use limit.");
            if (coupon.MinOrderAmount.HasValue && price.Amount < coupon.MinOrderAmount.Value)
                throw new DomainException(
                    $"Coupon '{request.CouponCode}' requires a minimum order amount of {coupon.MinOrderAmount.Value:0.00} {price.CurrencyCode}.");

            discountAmount = coupon.DiscountType == DiscountType.Percentage
                ? Math.Round(price.Amount * coupon.DiscountValue / 100, 2)
                : coupon.DiscountValue;

            // Cap: final price must be at least 1 unit of currency
            var projected = price.Amount - discountAmount;
            if (projected < 1)
                discountAmount = price.Amount - 1;
        }

        var amountPaid = price.Amount - discountAmount;
        var gateway = _gatewayFactory.GetGateway(locationCode);
        var gatewayEnum = locationCode == "IN" ? PaymentGateway.CashFree : PaymentGateway.PayPal;

        // ── 5. Persist order (Pending) ───────────────────────────────────────
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DurationId = request.DurationId,
            DurationPriceId = price.Id,
            AmountPaid = amountPaid,
            CurrencyCode = price.CurrencyCode,
            LocationCode = locationCode,
            CouponId = coupon?.Id,
            DiscountAmount = discountAmount,
            Status = OrderStatus.Pending,
            PaymentGateway = gatewayEnum,
            IdempotencyKey = request.IdempotencyKey,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _orders.AddAsync(order);

        if (coupon is not null)
        {
            coupon.UsedCount++;
            _coupons.Update(coupon);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // ── 6. Create external order on gateway ──────────────────────────────
        var gatewayRequest = new CreateGatewayOrderRequest(
            InternalOrderId: order.Id.ToString(),
            Amount: amountPaid,
            CurrencyCode: price.CurrencyCode,
            CustomerEmail: user.Email,
            CustomerName: $"{user.FirstName} {user.LastName}",
            CustomerPhone: user.FullMobile);

        GatewayCreateOrderResult gatewayResult;
        try
        {
            gatewayResult = await gateway.CreateOrderAsync(gatewayRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway {Gateway} failed to create order {OrderId}", gatewayEnum, order.Id);
            order.Status = OrderStatus.Failed;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            _orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);
            throw new DomainException("Payment gateway is unavailable. Please try again shortly.");
        }

        // ── 7. Store gateway reference ───────────────────────────────────────
        order.GatewayOrderId = gatewayResult.GatewayOrderId;
        order.GatewayResponse = gatewayResult.PaymentSessionId ?? gatewayResult.ApprovalUrl;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        _orders.Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} created via {Gateway}", order.Id, gatewayEnum);

        return new InitiateOrderResponse(
            OrderId: order.Id,
            Gateway: gatewayEnum.ToString().ToUpperInvariant(),
            Amount: amountPaid,
            Currency: price.CurrencyCode,
            Symbol: price.CurrencySymbol,
            GatewayOrderId: gatewayResult.GatewayOrderId,
            PaymentSessionId: gatewayResult.PaymentSessionId,
            ApprovalUrl: gatewayResult.ApprovalUrl);
    }

    private static InitiateOrderResponse BuildResponseFromExistingOrder(Order order) =>
        new(
            OrderId: order.Id,
            Gateway: order.PaymentGateway.ToString().ToUpperInvariant(),
            Amount: order.AmountPaid,
            Currency: order.CurrencyCode,
            Symbol: string.Empty,       // symbol not stored on order; front-end can derive from currency
            GatewayOrderId: order.GatewayOrderId,
            PaymentSessionId: order.PaymentGateway == PaymentGateway.CashFree
                ? order.GatewayResponse
                : null,
            ApprovalUrl: order.PaymentGateway == PaymentGateway.PayPal
                ? order.GatewayResponse
                : null);
}
