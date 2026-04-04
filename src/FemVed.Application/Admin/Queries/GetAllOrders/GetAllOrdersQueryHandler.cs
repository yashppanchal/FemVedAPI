using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAllOrders;

/// <summary>
/// Handles <see cref="GetAllOrdersQuery"/>.
/// Returns all orders across all users, enriched with user name, program name, duration label, and coupon code.
/// Uses batch repository calls to avoid N+1 queries while respecting the Application/Infrastructure boundary.
/// </summary>
public sealed class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, List<AdminOrderDto>>
{
    private readonly IRepository<Order>           _orders;
    private readonly IRepository<User>            _users;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Program>         _programs;
    private readonly IRepository<Coupon>          _coupons;
    private readonly ILogger<GetAllOrdersQueryHandler> _logger;

    /// <summary>Initialises the handler with all required repositories.</summary>
    public GetAllOrdersQueryHandler(
        IRepository<Order>           orders,
        IRepository<User>            users,
        IRepository<ProgramDuration> durations,
        IRepository<Program>         programs,
        IRepository<Coupon>          coupons,
        ILogger<GetAllOrdersQueryHandler> logger)
    {
        _orders    = orders;
        _users     = users;
        _durations = durations;
        _programs  = programs;
        _coupons   = coupons;
        _logger    = logger;
    }

    /// <summary>Returns all orders ordered by creation date descending, with joined user/program/duration/coupon data.</summary>
    public async Task<List<AdminOrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllOrders: loading all orders");

        var orders = await _orders.GetAllAsync(cancellationToken: cancellationToken);

        if (orders.Count == 0)
        {
            _logger.LogInformation("GetAllOrders: no orders found");
            return [];
        }

        // Collect distinct IDs for batch loading
        var userIds     = orders.Select(o => o.UserId).Distinct().ToHashSet();
        var durationIds = orders.Select(o => o.DurationId).Distinct().ToHashSet();
        var couponIds   = orders.Where(o => o.CouponId.HasValue).Select(o => o.CouponId!.Value).Distinct().ToHashSet();

        // Load related entities sequentially — EF Core DbContext is not thread-safe,
        // so concurrent Task.WhenAll calls on the same context cause InvalidOperationException.
        var users     = await _users.GetAllAsync(u => userIds.Contains(u.Id), cancellationToken);
        var durations = await _durations.GetAllAsync(d => durationIds.Contains(d.Id), cancellationToken);
        var coupons   = couponIds.Count > 0
            ? await _coupons.GetAllAsync(c => couponIds.Contains(c.Id), cancellationToken)
            : (IReadOnlyList<Coupon>)Array.Empty<Coupon>();

        var userMap     = users.ToDictionary(u => u.Id);
        var durationMap = durations.ToDictionary(d => d.Id);
        var couponMap   = coupons.ToDictionary(c => c.Id);

        // Batch load programs via duration ProgramIds
        var programIds = durationMap.Values.Select(d => d.ProgramId).Distinct().ToHashSet();
        var programs   = await _programs.GetAllAsync(p => programIds.Contains(p.Id), cancellationToken);
        var programMap = programs.ToDictionary(p => p.Id);

        var result = orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o =>
            {
                userMap.TryGetValue(o.UserId, out var user);
                durationMap.TryGetValue(o.DurationId.GetValueOrDefault(), out var duration);
                var programName = duration is not null && programMap.TryGetValue(duration.ProgramId, out var prog)
                    ? prog.Name ?? string.Empty
                    : string.Empty;
                var couponCode = o.CouponId.HasValue && couponMap.TryGetValue(o.CouponId.Value, out var coupon)
                    ? coupon.Code
                    : null;

                return new AdminOrderDto(
                    OrderId:        o.Id,
                    UserId:         o.UserId,
                    UserName:       user is not null ? $"{user.FirstName} {user.LastName}".Trim() : string.Empty,
                    UserEmail:      user?.Email ?? string.Empty,
                    ProgramId:      duration?.ProgramId ?? Guid.Empty,
                    ProgramName:    programName,
                    DurationId:     o.DurationId.GetValueOrDefault(),
                    DurationLabel:  duration?.Label ?? string.Empty,
                    Amount:         o.AmountPaid,
                    Currency:       o.CurrencyCode,
                    DiscountAmount: o.DiscountAmount,
                    CouponCode:     couponCode,
                    Status:         o.Status.ToString(),
                    Gateway:        o.PaymentGateway.ToString(),
                    GatewayOrderId: o.GatewayOrderId,
                    CreatedAt:      o.CreatedAt
                );
            })
            .ToList();

        _logger.LogInformation("GetAllOrders: returned {Count} orders", result.Count);
        return result;
    }
}
