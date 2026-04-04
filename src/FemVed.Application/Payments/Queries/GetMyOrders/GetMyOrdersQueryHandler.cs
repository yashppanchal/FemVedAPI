using FemVed.Application.Interfaces;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Payments.Queries.GetMyOrders;

/// <summary>
/// Handles <see cref="GetMyOrdersQuery"/>.
/// Returns all orders for the requesting user, enriched with program name,
/// duration label, and coupon code, ordered by creation date descending.
/// </summary>
public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, List<OrderDto>>
{
    private readonly IRepository<Order> _orders;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Program> _programs;
    private readonly IRepository<Coupon> _coupons;
    private readonly ILogger<GetMyOrdersQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetMyOrdersQueryHandler(
        IRepository<Order> orders,
        IRepository<ProgramDuration> durations,
        IRepository<Program> programs,
        IRepository<Coupon> coupons,
        ILogger<GetMyOrdersQueryHandler> logger)
    {
        _orders    = orders;
        _durations = durations;
        _programs  = programs;
        _coupons   = coupons;
        _logger    = logger;
    }

    /// <summary>Returns all orders for the authenticated user.</summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of order DTOs, newest first.</returns>
    public async Task<List<OrderDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyOrders: loading orders for user {UserId}", request.UserId);

        var orders = await _orders.GetAllAsync(
            o => o.UserId == request.UserId,
            cancellationToken);

        if (orders.Count == 0)
        {
            _logger.LogInformation("GetMyOrders: no orders for user {UserId}", request.UserId);
            return new List<OrderDto>();
        }

        // Batch-load related entities
        var durationIds = orders.Select(o => o.DurationId).ToHashSet();
        var couponIds   = orders.Where(o => o.CouponId.HasValue).Select(o => o.CouponId!.Value).ToHashSet();

        var durations = (await _durations.GetAllAsync(d => durationIds.Contains(d.Id), cancellationToken))
            .ToDictionary(d => d.Id);

        var programIds = durations.Values.Select(d => d.ProgramId).ToHashSet();
        var programs   = (await _programs.GetAllAsync(p => programIds.Contains(p.Id), cancellationToken))
            .ToDictionary(p => p.Id);

        var coupons = couponIds.Count > 0
            ? (await _coupons.GetAllAsync(c => couponIds.Contains(c.Id), cancellationToken))
                .ToDictionary(c => c.Id)
            : new Dictionary<Guid, Coupon>();

        var result = orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o =>
            {
                durations.TryGetValue(o.DurationId.GetValueOrDefault(), out var duration);
                var programId = duration?.ProgramId;
                programs.TryGetValue(programId ?? Guid.Empty, out var program);
                coupons.TryGetValue(o.CouponId ?? Guid.Empty, out var coupon);

                return new OrderDto(
                    OrderId:        o.Id,
                    UserId:         o.UserId,
                    ProgramId:      programId,
                    ProgramName:    program?.Name,
                    DurationId:     o.DurationId.GetValueOrDefault(),
                    DurationLabel:  duration?.Label ?? string.Empty,
                    Amount:         o.AmountPaid,
                    Currency:       o.CurrencyCode,
                    LocationCode:   o.LocationCode,
                    CouponCode:     coupon?.Code,
                    DiscountAmount: o.DiscountAmount,
                    Status:         o.Status.ToString(),
                    Gateway:        o.PaymentGateway.ToString().ToUpperInvariant(),
                    GatewayOrderId: o.GatewayOrderId,
                    FailureReason:  o.FailureReason,
                    CreatedAt:      o.CreatedAt);
            })
            .ToList();

        _logger.LogInformation("GetMyOrders: returned {Count} orders for user {UserId}", result.Count, request.UserId);

        return result;
    }
}
