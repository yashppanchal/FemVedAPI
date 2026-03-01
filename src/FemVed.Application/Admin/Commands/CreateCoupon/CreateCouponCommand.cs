using FemVed.Application.Admin.DTOs;
using FemVed.Domain.Enums;
using MediatR;

namespace FemVed.Application.Admin.Commands.CreateCoupon;

/// <summary>Creates a new discount coupon. Returns the created <see cref="CouponDto"/>.</summary>
public record CreateCouponCommand(
    Guid AdminUserId,
    string? IpAddress,
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinOrderAmount,
    int? MaxUses,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil) : IRequest<CouponDto>;
