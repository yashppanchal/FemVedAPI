using FemVed.Application.Interfaces;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Queries.GetOrder;

/// <summary>
/// Handles <see cref="GetOrderQuery"/>.
/// Returns the order DTO, enforcing ownership rules for non-Admin callers.
/// Enriches the response with program name, duration label, and coupon code
/// by loading the related <see cref="ProgramDuration"/>, <see cref="Program"/>,
/// and <see cref="Coupon"/> records.
/// </summary>
public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDto>
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Program> _programs;
    private readonly IRepository<Coupon> _coupons;
    private readonly ILogger<GetOrderQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetOrderQueryHandler(
        IRepository<Order> orders,
        IRepository<ProgramDuration> durations,
        IRepository<Program> programs,
        IRepository<Coupon> coupons,
        ILogger<GetOrderQueryHandler> logger)
    {
        _orders    = orders;
        _durations = durations;
        _programs  = programs;
        _coupons   = coupons;
        _logger    = logger;
    }

    /// <summary>Returns the enriched order DTO for the given order ID.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Order DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the order is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when a non-Admin user requests another user's order.</exception>
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching order {OrderId} for user {UserId}", request.OrderId, request.RequestingUserId);

        var order = await _orders.FirstOrDefaultAsync(
            o => o.Id == request.OrderId,
            cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (!request.IsAdmin && order.UserId != request.RequestingUserId)
            throw new ForbiddenException("You do not have permission to view this order.");

        // Enrich with related data
        var duration = await _durations.FirstOrDefaultAsync(d => d.Id == order.DurationId, cancellationToken);
        Program? program = null;
        if (duration is not null)
            program = await _programs.FirstOrDefaultAsync(p => p.Id == duration.ProgramId, cancellationToken);

        Coupon? coupon = null;
        if (order.CouponId.HasValue)
            coupon = await _coupons.FirstOrDefaultAsync(c => c.Id == order.CouponId.Value, cancellationToken);

        _logger.LogInformation("Order {OrderId} returned successfully", request.OrderId);

        return new OrderDto(
            OrderId:        order.Id,
            UserId:         order.UserId,
            ProgramId:      program?.Id,
            ProgramName:    program?.Name,
            DurationId:     order.DurationId.GetValueOrDefault(),
            DurationLabel:  duration?.Label ?? string.Empty,
            Amount:         order.AmountPaid,
            Currency:       order.CurrencyCode,
            LocationCode:   order.LocationCode,
            CouponCode:     coupon?.Code,
            DiscountAmount: order.DiscountAmount,
            Status:         order.Status.ToString(),
            Gateway:        order.PaymentGateway.ToString().ToUpperInvariant(),
            GatewayOrderId: order.GatewayOrderId,
            FailureReason:  order.FailureReason,
            CreatedAt:      order.CreatedAt);
    }
}
