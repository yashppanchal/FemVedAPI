using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAllCoupons;

/// <summary>Handles <see cref="GetAllCouponsQuery"/>.</summary>
public sealed class GetAllCouponsQueryHandler : IRequestHandler<GetAllCouponsQuery, List<CouponDto>>
{
    private readonly IRepository<Coupon> _coupons;
    private readonly ILogger<GetAllCouponsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllCouponsQueryHandler(IRepository<Coupon> coupons, ILogger<GetAllCouponsQueryHandler> logger)
    {
        _coupons = coupons;
        _logger  = logger;
    }

    /// <summary>Returns all coupons ordered by creation date descending.</summary>
    public async Task<List<CouponDto>> Handle(GetAllCouponsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllCoupons: loading all coupons");

        var coupons = await _coupons.GetAllAsync(cancellationToken: cancellationToken);

        var result = coupons
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CouponDto(
                CouponId:        c.Id,
                Code:            c.Code,
                DiscountType:    c.DiscountType.ToString(),
                DiscountValue:   c.DiscountValue,
                MinOrderAmount:  c.MinOrderAmount,
                MaxUses:         c.MaxUses,
                UsedCount:       c.UsedCount,
                ValidFrom:       c.ValidFrom,
                ValidUntil:      c.ValidUntil,
                IsActive:        c.IsActive,
                CreatedAt:       c.CreatedAt,
                UpdatedAt:       c.UpdatedAt))
            .ToList();

        _logger.LogInformation("GetAllCoupons: returned {Count} coupons", result.Count);
        return result;
    }
}
