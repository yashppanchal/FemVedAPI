using FemVed.Application.Interfaces;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Program = FemVed.Domain.Entities.Program;

namespace FemVed.Application.Payments.Commands.InitiateOrder;

/// <summary>
/// Handles <see cref="InitiateOrderCommand"/>.
/// Supports two purchase flows:
/// <list type="bullet">
///   <item><b>Guided</b> — when <c>DurationId</c> is provided: validates the duration, loads the program, resolves price from <c>duration_prices</c>.</item>
///   <item><b>Library</b> — when <c>VideoId</c> is provided: validates the video, checks it's PUBLISHED, resolves price from per-video override or tier default.</item>
/// </list>
/// Both paths share: idempotency check, coupon validation, gateway creation, and order persistence.
/// </summary>
public sealed class InitiateOrderCommandHandler
    : IRequestHandler<InitiateOrderCommand, InitiateOrderResponse>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryVideoPrice> _videoPrices;
    private readonly IRepository<LibraryTierPrice> _tierPrices;
    private readonly IRepository<Coupon> _coupons;
    private readonly IRepository<Order> _orders;
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<UserLibraryAccess> _libraryAccess;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InitiateOrderCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public InitiateOrderCommandHandler(
        IRepository<User> users,
        IRepository<Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        IRepository<LibraryVideo> videos,
        IRepository<LibraryVideoPrice> videoPrices,
        IRepository<LibraryTierPrice> tierPrices,
        IRepository<Coupon> coupons,
        IRepository<Order> orders,
        IRepository<UserProgramAccess> access,
        IRepository<UserLibraryAccess> libraryAccess,
        IPaymentGatewayFactory gatewayFactory,
        IUnitOfWork uow,
        ILogger<InitiateOrderCommandHandler> logger)
    {
        _users = users;
        _programs = programs;
        _durations = durations;
        _prices = prices;
        _videos = videos;
        _videoPrices = videoPrices;
        _tierPrices = tierPrices;
        _coupons = coupons;
        _orders = orders;
        _access = access;
        _libraryAccess = libraryAccess;
        _gatewayFactory = gatewayFactory;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Processes the order initiation request.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Gateway-specific order tokens and metadata.</returns>
    /// <exception cref="NotFoundException">Thrown if user, duration/video, or price record is not found.</exception>
    /// <exception cref="DomainException">
    /// Thrown when the program/video is not published, the user already has access,
    /// the coupon is invalid/expired/exhausted, or no price exists for the user's location.
    /// </exception>
    public async Task<InitiateOrderResponse> Handle(
        InitiateOrderCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Initiating order for user {UserId}, DurationId={DurationId}, VideoId={VideoId}",
            request.UserId, request.DurationId, request.VideoId);

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

        var locationCode = request.CountryCode?.ToString() ?? "GB";

        // ── 3. Branch: Guided vs Library ─────────────────────────────────────
        OrderSource orderSource;
        Guid? durationId = null;
        Guid? durationPriceId = null;
        Guid? libraryVideoId = null;
        decimal baseAmount;
        string currencyCode;
        string currencySymbol;
        string couponScope;

        if (request.DurationId.HasValue)
        {
            // ── GUIDED FLOW ──────────────────────────────────────────────────
            orderSource = OrderSource.Guided;
            durationId = request.DurationId.Value;

            var duration = await _durations.FirstOrDefaultAsync(
                d => d.Id == request.DurationId.Value && d.IsActive,
                cancellationToken)
                ?? throw new NotFoundException("ProgramDuration", request.DurationId.Value);

            var program = await _programs.FirstOrDefaultAsync(
                p => p.Id == duration.ProgramId && !p.IsDeleted,
                cancellationToken)
                ?? throw new NotFoundException("Program", duration.ProgramId);

            if (program.Status != ProgramStatus.Published)
                throw new DomainException("This program is not currently available for purchase.");

            // Prevent duplicate active enrollment
            var alreadyHasAccess = await _access.AnyAsync(
                a => a.UserId == request.UserId
                  && a.ProgramId == duration.ProgramId
                  && a.Status != UserProgramAccessStatus.Completed
                  && a.Status != UserProgramAccessStatus.Cancelled,
                cancellationToken);

            if (alreadyHasAccess)
                throw new DomainException(
                    "You already have an active enrollment in this program. " +
                    "Complete or cancel your current enrollment before purchasing again.");

            var price = await _prices.FirstOrDefaultAsync(
                dp => dp.DurationId == request.DurationId.Value
                   && dp.LocationCode == locationCode
                   && dp.IsActive,
                cancellationToken)
                ?? await _prices.FirstOrDefaultAsync(
                    dp => dp.DurationId == request.DurationId.Value
                       && dp.LocationCode == "GB"
                       && dp.IsActive,
                    cancellationToken)
                ?? throw new DomainException(
                    $"No price is available for duration {request.DurationId.Value} in location '{locationCode}'.");

            durationPriceId = price.Id;
            baseAmount = price.Amount;
            currencyCode = price.CurrencyCode;
            currencySymbol = price.CurrencySymbol;
            couponScope = "GUIDED";
        }
        else
        {
            // ── LIBRARY FLOW ─────────────────────────────────────────────────
            orderSource = OrderSource.Library;
            libraryVideoId = request.VideoId!.Value;

            var video = await _videos.FirstOrDefaultAsync(
                v => v.Id == request.VideoId.Value && !v.IsDeleted,
                cancellationToken)
                ?? throw new NotFoundException("LibraryVideo", request.VideoId.Value);

            if (video.Status != VideoStatus.Published)
                throw new DomainException("This video is not currently available for purchase.");

            // Prevent duplicate purchase
            var alreadyOwns = await _libraryAccess.AnyAsync(
                a => a.UserId == request.UserId
                  && a.VideoId == request.VideoId.Value
                  && a.IsActive,
                cancellationToken);

            if (alreadyOwns)
                throw new DomainException("You already own this video.");

            // Price resolution: per-video override → tier default
            var videoPrice = await _videoPrices.FirstOrDefaultAsync(
                p => p.VideoId == video.Id && p.LocationCode == locationCode,
                cancellationToken)
                ?? await _videoPrices.FirstOrDefaultAsync(
                    p => p.VideoId == video.Id && p.LocationCode == "GB",
                    cancellationToken);

            if (videoPrice is not null)
            {
                baseAmount = videoPrice.Amount;
                currencyCode = videoPrice.CurrencyCode;
                currencySymbol = videoPrice.CurrencySymbol;
            }
            else
            {
                // Fall back to tier price
                var tierPrice = await _tierPrices.FirstOrDefaultAsync(
                    tp => tp.TierId == video.PriceTierId && tp.LocationCode == locationCode,
                    cancellationToken)
                    ?? await _tierPrices.FirstOrDefaultAsync(
                        tp => tp.TierId == video.PriceTierId && tp.LocationCode == "GB",
                        cancellationToken)
                    ?? throw new DomainException(
                        $"No price is available for video '{video.Title}' in location '{locationCode}'.");

                baseAmount = tierPrice.Amount;
                currencyCode = tierPrice.CurrencyCode;
                currencySymbol = tierPrice.CurrencySymbol;
            }

            couponScope = "LIBRARY";
        }

        // ── 4. Apply coupon ──────────────────────────────────────────────────
        decimal discountAmount = 0;
        Coupon? coupon = null;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            coupon = await _coupons.FirstOrDefaultAsync(
                c => c.Code == request.CouponCode && c.IsActive,
                cancellationToken)
                ?? throw new DomainException($"Coupon '{request.CouponCode}' is invalid or does not exist.");

            // Validate coupon scope
            if (coupon.Scope != "ALL" && !string.Equals(coupon.Scope, couponScope, StringComparison.OrdinalIgnoreCase))
                throw new DomainException(
                    $"Coupon '{request.CouponCode}' is not valid for {couponScope.ToLowerInvariant()} purchases.");

            var now = DateTimeOffset.UtcNow;
            if (coupon.ValidFrom.HasValue && now < coupon.ValidFrom)
                throw new DomainException($"Coupon '{request.CouponCode}' is not yet valid.");
            if (coupon.ValidUntil.HasValue && now > coupon.ValidUntil)
                throw new DomainException($"Coupon '{request.CouponCode}' has expired.");
            if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses)
                throw new DomainException($"Coupon '{request.CouponCode}' has reached its maximum use limit.");
            if (coupon.MinOrderAmount.HasValue && baseAmount < coupon.MinOrderAmount.Value)
                throw new DomainException(
                    $"Coupon '{request.CouponCode}' requires a minimum order amount of {coupon.MinOrderAmount.Value:0.00} {currencyCode}.");

            discountAmount = coupon.DiscountType == DiscountType.Percentage
                ? Math.Round(baseAmount * coupon.DiscountValue / 100, 2)
                : coupon.DiscountValue;

            // Cap: final price must be at least 1 unit of currency
            var projected = baseAmount - discountAmount;
            if (projected < 1)
                discountAmount = baseAmount - 1;
        }

        var amountPaid = baseAmount - discountAmount;

        // Priority: (1) explicit request body gateway, (2) default PayPal
        var gatewayEnum = request.Gateway ?? PaymentGateway.PayPal;

        // Server-side enforcement: Stripe does not support INR — India must use CashFree
        if (gatewayEnum == PaymentGateway.Stripe && locationCode == "IN")
            throw new DomainException("Stripe is not available for Indian customers. Please use CashFree.");

        var gateway = _gatewayFactory.GetGatewayByType(gatewayEnum);

        // ── 5. Persist order (Pending) ───────────────────────────────────────
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DurationId = durationId,
            DurationPriceId = durationPriceId,
            LibraryVideoId = libraryVideoId,
            OrderSource = orderSource,
            AmountPaid = amountPaid,
            CurrencyCode = currencyCode,
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
            CurrencyCode: currencyCode,
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

        _logger.LogInformation("Order {OrderId} created via {Gateway} (source={Source})",
            order.Id, gatewayEnum, orderSource);

        return new InitiateOrderResponse(
            OrderId: order.Id,
            Gateway: gatewayEnum.ToString().ToUpperInvariant(),
            Amount: amountPaid,
            Currency: currencyCode,
            Symbol: currencySymbol,
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
            ApprovalUrl: order.PaymentGateway == PaymentGateway.PayPal || order.PaymentGateway == PaymentGateway.Stripe
                ? order.GatewayResponse
                : null);
}
