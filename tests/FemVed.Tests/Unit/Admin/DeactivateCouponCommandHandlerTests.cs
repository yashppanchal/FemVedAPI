using FemVed.Application.Admin.Commands.DeactivateCoupon;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Admin;

public class DeactivateCouponCommandHandlerTests
{
    private readonly Mock<IRepository<Coupon>> _coupons = new();
    private readonly Mock<IRepository<AdminAuditLog>> _auditLogs = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private DeactivateCouponCommandHandler CreateHandler() =>
        new(_coupons.Object, _auditLogs.Object, _uow.Object,
            NullLogger<DeactivateCouponCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ExistingCoupon_SetsIsActiveFalse()
    {
        // Arrange
        var couponId = Guid.NewGuid();
        var coupon = new Coupon { Id = couponId, Code = "SAVE20", IsActive = true };

        _coupons.Setup(r => r.FirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupon);

        _coupons.Setup(r => r.Update(It.IsAny<Coupon>()));
        _auditLogs.Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var cmd = new DeactivateCouponCommand(Guid.NewGuid(), "127.0.0.1", couponId);
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.False(coupon.IsActive);
        _coupons.Verify(r => r.Update(coupon), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CouponNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _coupons.Setup(r => r.FirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Coupon?)null);

        var cmd = new DeactivateCouponCommand(Guid.NewGuid(), "127.0.0.1", Guid.NewGuid());
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingCoupon_WritesAuditLog()
    {
        // Arrange
        var couponId = Guid.NewGuid();
        var adminId  = Guid.NewGuid();
        var coupon   = new Coupon { Id = couponId, Code = "TEST10", IsActive = true };

        _coupons.Setup(r => r.FirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupon);

        _coupons.Setup(r => r.Update(It.IsAny<Coupon>()));
        _auditLogs.Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var cmd = new DeactivateCouponCommand(adminId, "10.0.0.1", couponId);
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        _auditLogs.Verify(r => r.AddAsync(It.Is<AdminAuditLog>(l =>
            l.Action      == "DEACTIVATE_COUPON" &&
            l.EntityType  == "coupons" &&
            l.AdminUserId == adminId &&
            l.EntityId    == couponId)), Times.Once);
    }
}
