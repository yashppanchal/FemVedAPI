using System.Text.Json;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.UpdateCoupon;

/// <summary>Handles <see cref="UpdateCouponCommand"/>. Applies partial updates and writes an audit log entry.</summary>
public sealed class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, CouponDto>
{
    private readonly IRepository<Coupon> _coupons;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateCouponCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public UpdateCouponCommandHandler(
        IRepository<Coupon> coupons,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<UpdateCouponCommandHandler> logger)
    {
        _coupons   = coupons;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Updates the coupon and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the coupon does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the new code conflicts with another coupon.</exception>
    public async Task<CouponDto> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateCoupon: admin {AdminId} updating coupon {CouponId}", request.AdminUserId, request.CouponId);

        var coupon = await _coupons.FirstOrDefaultAsync(c => c.Id == request.CouponId, cancellationToken)
            ?? throw new NotFoundException(nameof(Coupon), request.CouponId);

        var before = JsonSerializer.Serialize(new
        {
            coupon.Code,
            DiscountType     = coupon.DiscountType.ToString(),
            coupon.DiscountValue,
            coupon.MinOrderAmount,
            coupon.MaxUses,
            coupon.ValidFrom,
            coupon.ValidUntil
        });

        if (request.Code is not null)
        {
            var newCode = request.Code.ToUpperInvariant();
            if (newCode != coupon.Code)
            {
                var codeConflict = await _coupons.AnyAsync(c => c.Code == newCode && c.Id != coupon.Id, cancellationToken);
                if (codeConflict)
                    throw new DomainException($"A coupon with code '{newCode}' already exists.");
                coupon.Code = newCode;
            }
        }

        if (request.DiscountType.HasValue)         coupon.DiscountType   = request.DiscountType.Value;
        if (request.DiscountValue.HasValue)        coupon.DiscountValue  = request.DiscountValue.Value;
        if (request.ClearMinOrderAmount)           coupon.MinOrderAmount = null;
        else if (request.MinOrderAmount.HasValue)  coupon.MinOrderAmount = request.MinOrderAmount.Value;
        if (request.ClearMaxUses)                  coupon.MaxUses        = null;
        else if (request.MaxUses.HasValue)         coupon.MaxUses        = request.MaxUses.Value;
        if (request.ClearValidFrom)          coupon.ValidFrom     = null;
        else if (request.ValidFrom.HasValue) coupon.ValidFrom     = request.ValidFrom.Value;
        if (request.ClearValidUntil)         coupon.ValidUntil    = null;
        else if (request.ValidUntil.HasValue) coupon.ValidUntil   = request.ValidUntil.Value;

        coupon.UpdatedAt = DateTimeOffset.UtcNow;
        _coupons.Update(coupon);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "UPDATE_COUPON",
            EntityType  = "coupons",
            EntityId    = coupon.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new
            {
                coupon.Code,
                DiscountType     = coupon.DiscountType.ToString(),
                coupon.DiscountValue,
                coupon.MinOrderAmount,
                coupon.MaxUses,
                coupon.ValidFrom,
                coupon.ValidUntil
            }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateCoupon: coupon {CouponId} updated", coupon.Id);

        return new CouponDto(
            coupon.Id,
            coupon.Code,
            coupon.DiscountType.ToString(),
            coupon.DiscountValue,
            coupon.MinOrderAmount,
            coupon.MaxUses,
            coupon.UsedCount,
            coupon.ValidFrom,
            coupon.ValidUntil,
            coupon.IsActive,
            coupon.CreatedAt,
            coupon.UpdatedAt);
    }
}
