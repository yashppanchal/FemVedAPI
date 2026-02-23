using MediatR;

namespace FemVed.Application.Admin.Commands.DeactivateCoupon;

/// <summary>Sets <c>IsActive = false</c> on the specified coupon and writes an audit log entry.</summary>
public record DeactivateCouponCommand(
    Guid AdminUserId,
    string? IpAddress,
    Guid CouponId) : IRequest;
