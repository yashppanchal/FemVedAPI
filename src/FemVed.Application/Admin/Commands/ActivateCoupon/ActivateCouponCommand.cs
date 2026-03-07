using MediatR;

namespace FemVed.Application.Admin.Commands.ActivateCoupon;

/// <summary>Sets <c>IsActive = true</c> on the specified coupon and writes an audit log entry.</summary>
public record ActivateCouponCommand(
    Guid AdminUserId,
    string? IpAddress,
    Guid CouponId) : IRequest;
