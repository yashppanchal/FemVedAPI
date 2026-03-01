using FemVed.Application.Admin.Commands.CreateCoupon;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Admin;

public class CreateCouponCommandHandlerTests
{
    private readonly Mock<IRepository<Coupon>> _coupons = new();
    private readonly Mock<IRepository<AdminAuditLog>> _auditLogs = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CreateCouponCommandHandler CreateHandler() =>
        new(_coupons.Object, _auditLogs.Object, _uow.Object,
            NullLogger<CreateCouponCommandHandler>.Instance);

    private static CreateCouponCommand ValidCommand() => new(
        AdminUserId:    Guid.NewGuid(),
        IpAddress:      "127.0.0.1",
        Code:           "SAVE20",
        DiscountType:   DiscountType.Percentage,
        DiscountValue:  20m,
        MinOrderAmount: null,
        MaxUses:        null,
        ValidFrom:      null,
        ValidUntil:     null);

    [Fact]
    public async Task Handle_NewCode_ReturnsCouponDto()
    {
        // Arrange
        _coupons.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        _coupons.Setup(r => r.AddAsync(It.IsAny<Coupon>())).Returns(Task.CompletedTask);
        _auditLogs.Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SAVE20", result.Code);
        Assert.Equal("Percentage", result.DiscountType);
        Assert.Equal(20m, result.DiscountValue);
        Assert.True(result.IsActive);
        Assert.Equal(0, result.UsedCount);
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsDomainException()
    {
        // Arrange
        _coupons.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // Code already exists

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(ValidCommand(), CancellationToken.None));

        Assert.Contains("SAVE20", ex.Message);
    }

    [Fact]
    public async Task Handle_NewCode_UppercasesCodeBeforeStore()
    {
        // Arrange
        Coupon? captured = null;
        _coupons.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        _coupons.Setup(r => r.AddAsync(It.IsAny<Coupon>()))
                .Callback<Coupon>(c => captured = c)
                .Returns(Task.CompletedTask);
        _auditLogs.Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var cmd = ValidCommand() with { Code = "save20" }; // lowercase input

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert — stored as uppercase
        Assert.NotNull(captured);
        Assert.Equal("SAVE20", captured!.Code);
    }

    [Fact]
    public async Task Handle_Success_WritesAuditLog()
    {
        // Arrange
        _coupons.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        _coupons.Setup(r => r.AddAsync(It.IsAny<Coupon>())).Returns(Task.CompletedTask);
        _auditLogs.Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert — audit log was written exactly once
        _auditLogs.Verify(r => r.AddAsync(It.Is<AdminAuditLog>(l =>
            l.Action == "CREATE_COUPON" && l.EntityType == "coupons")), Times.Once);
    }
}
