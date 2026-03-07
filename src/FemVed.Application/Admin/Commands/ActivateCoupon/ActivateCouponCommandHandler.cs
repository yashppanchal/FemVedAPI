using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.ActivateCoupon;

/// <summary>Handles <see cref="ActivateCouponCommand"/>. Sets IsActive = true and writes an audit log entry.</summary>
public sealed class ActivateCouponCommandHandler : IRequestHandler<ActivateCouponCommand>
{
    private readonly IRepository<Coupon> _coupons;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ActivateCouponCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public ActivateCouponCommandHandler(
        IRepository<Coupon> coupons,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<ActivateCouponCommandHandler> logger)
    {
        _coupons   = coupons;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Activates the coupon and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the coupon does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the coupon is already active.</exception>
    public async Task Handle(ActivateCouponCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ActivateCoupon: admin {AdminId} activating coupon {CouponId}", request.AdminUserId, request.CouponId);

        var coupon = await _coupons.FirstOrDefaultAsync(c => c.Id == request.CouponId, cancellationToken)
            ?? throw new NotFoundException(nameof(Coupon), request.CouponId);

        if (coupon.IsActive)
            throw new DomainException("Coupon is already active.");

        var before = JsonSerializer.Serialize(new { coupon.IsActive });
        coupon.IsActive  = true;
        coupon.UpdatedAt = DateTimeOffset.UtcNow;
        _coupons.Update(coupon);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "ACTIVATE_COUPON",
            EntityType  = "coupons",
            EntityId    = coupon.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsActive = true }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ActivateCoupon: coupon {CouponId} activated", coupon.Id);
    }
}
