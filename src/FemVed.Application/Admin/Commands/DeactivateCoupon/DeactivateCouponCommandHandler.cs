using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.DeactivateCoupon;

/// <summary>Handles <see cref="DeactivateCouponCommand"/>. Sets IsActive = false and writes an audit log entry.</summary>
public sealed class DeactivateCouponCommandHandler : IRequestHandler<DeactivateCouponCommand>
{
    private readonly IRepository<Coupon> _coupons;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeactivateCouponCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeactivateCouponCommandHandler(
        IRepository<Coupon> coupons,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<DeactivateCouponCommandHandler> logger)
    {
        _coupons   = coupons;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Deactivates the coupon and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the coupon does not exist.</exception>
    public async Task Handle(DeactivateCouponCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeactivateCoupon: admin {AdminId} deactivating coupon {CouponId}", request.AdminUserId, request.CouponId);

        var coupon = await _coupons.FirstOrDefaultAsync(c => c.Id == request.CouponId, cancellationToken)
            ?? throw new NotFoundException(nameof(Coupon), request.CouponId);

        var before = JsonSerializer.Serialize(new { coupon.IsActive });
        coupon.IsActive  = false;
        coupon.UpdatedAt = DateTimeOffset.UtcNow;
        _coupons.Update(coupon);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "DEACTIVATE_COUPON",
            EntityType  = "coupons",
            EntityId    = coupon.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsActive = false }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DeactivateCoupon: coupon {CouponId} deactivated", coupon.Id);
    }
}
