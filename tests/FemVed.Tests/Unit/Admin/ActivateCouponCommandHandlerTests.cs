using FemVed.Application.Admin.Commands.ActivateCoupon;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Admin;

public class ActivateCouponCommandHandlerTests
{
    private readonly Mock<IRepository<Coupon>> _coupons = new();
    private readonly Mock<IRepository<AdminAuditLog>> _auditLogs = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ActivateCouponCommandHandler CreateHandler() =>
        new(_coupons.Object, _auditLogs.Object, _uow.Object,
            NullLogger<ActivateCouponCommandHandler>.Instance);

    private static Coupon MakeCoupon(bool isActive = false) => new()
    {
        Id            = Guid.NewGuid(),
        Code          = "PROMO10",
        DiscountType  = DiscountType.Percentage,
        DiscountValue = 10m,
        IsActive      = isActive,
        UsedCount     = 0
    };

    [Fact]
    public async Task Handle_InactiveCoupon_SetsIsActiveTrue()
    {
        // Arrange
        var coupon = MakeCoupon(isActive: false);
        _coupons.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupon);
        _auditLogs.Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(
            new ActivateCouponCommand(Guid.NewGuid(), "127.0.0.1", coupon.Id),
            CancellationToken.None);

        // Assert
        Assert.True(coupon.IsActive);
        _coupons.Verify(r => r.Update(It.Is<Coupon>(c => c.IsActive == true)), Times.Once);
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

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                new ActivateCouponCommand(Guid.NewGuid(), null, Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyActiveCoupon_ThrowsDomainException()
    {
        // Arrange
        var coupon = MakeCoupon(isActive: true);  // already active
        _coupons.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupon);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                new ActivateCouponCommand(Guid.NewGuid(), null, coupon.Id),
                CancellationToken.None));

        Assert.Contains("already active", ex.Message);
    }

    [Fact]
    public async Task Handle_Success_WritesActivateAuditLog()
    {
        // Arrange
        var coupon = MakeCoupon(isActive: false);
        _coupons.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupon);
        _auditLogs.Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(
            new ActivateCouponCommand(Guid.NewGuid(), "10.0.0.1", coupon.Id),
            CancellationToken.None);

        // Assert — audit log entry written with ACTIVATE_COUPON action
        _auditLogs.Verify(r => r.AddAsync(It.Is<AdminAuditLog>(l =>
            l.Action == "ACTIVATE_COUPON" && l.EntityType == "coupons")), Times.Once);
    }
}
