using System.Text.Json;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.CreateCoupon;

/// <summary>Handles <see cref="CreateCouponCommand"/>. Creates a new coupon and writes an audit log entry.</summary>
public sealed class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, CouponDto>
{
    private readonly IRepository<Coupon> _coupons;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateCouponCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public CreateCouponCommandHandler(
        IRepository<Coupon> coupons,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<CreateCouponCommandHandler> logger)
    {
        _coupons   = coupons;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Creates the coupon and logs the action.</summary>
    /// <exception cref="DomainException">Thrown when a coupon with the same code already exists.</exception>
    public async Task<CouponDto> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateCoupon: admin {AdminId} creating coupon {Code}", request.AdminUserId, request.Code);

        var code = request.Code.ToUpperInvariant();

        var exists = await _coupons.AnyAsync(c => c.Code == code, cancellationToken);
        if (exists)
            throw new DomainException($"A coupon with code '{code}' already exists.");

        var coupon = new Coupon
        {
            Id            = Guid.NewGuid(),
            Code          = code,
            DiscountType  = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MaxUses       = request.MaxUses,
            UsedCount     = 0,
            ValidFrom     = request.ValidFrom,
            ValidUntil    = request.ValidUntil,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };

        await _coupons.AddAsync(coupon);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "CREATE_COUPON",
            EntityType  = "coupons",
            EntityId    = coupon.Id,
            BeforeValue = null,
            AfterValue  = JsonSerializer.Serialize(new { coupon.Code, DiscountType = coupon.DiscountType.ToString(), coupon.DiscountValue, coupon.MaxUses, coupon.ValidFrom, coupon.ValidUntil }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("CreateCoupon: coupon {CouponId} ({Code}) created", coupon.Id, coupon.Code);

        return new CouponDto(
            coupon.Id,
            coupon.Code,
            coupon.DiscountType.ToString(),
            coupon.DiscountValue,
            coupon.MaxUses,
            coupon.UsedCount,
            coupon.ValidFrom,
            coupon.ValidUntil,
            coupon.IsActive,
            coupon.CreatedAt,
            coupon.UpdatedAt);
    }
}
