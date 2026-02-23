using FemVed.Application.Admin.DTOs;
using FemVed.Domain.Enums;
using MediatR;

namespace FemVed.Application.Admin.Commands.UpdateCoupon;

/// <summary>Updates an existing coupon. Only non-null fields are applied. Returns the updated <see cref="CouponDto"/>.</summary>
public record UpdateCouponCommand(
    Guid AdminUserId,
    string? IpAddress,
    Guid CouponId,
    string? Code,
    DiscountType? DiscountType,
    decimal? DiscountValue,
    int? MaxUses,
    bool ClearMaxUses,
    DateTimeOffset? ValidFrom,
    bool ClearValidFrom,
    DateTimeOffset? ValidUntil,
    bool ClearValidUntil) : IRequest<CouponDto>;
