using FemVed.Application.Interfaces;
using FemVed.Application.Payments.Commands.InitiateOrder;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Program = FemVed.Domain.Entities.Program;

namespace FemVed.Tests.Unit.Payments;

public class InitiateOrderCommandHandlerTests
{
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IRepository<Program>> _programs = new();
    private readonly Mock<IRepository<ProgramDuration>> _durations = new();
    private readonly Mock<IRepository<DurationPrice>> _prices = new();
    private readonly Mock<IRepository<Coupon>> _coupons = new();
    private readonly Mock<IRepository<Order>> _orders = new();
    private readonly Mock<IRepository<UserProgramAccess>> _access = new();
    private readonly Mock<IPaymentGatewayFactory> _gatewayFactory = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private InitiateOrderCommandHandler CreateHandler() =>
        new(_users.Object, _programs.Object, _durations.Object, _prices.Object,
            _coupons.Object, _orders.Object, _access.Object,
            _gatewayFactory.Object, _uow.Object,
            NullLogger<InitiateOrderCommandHandler>.Instance);

    // ── Shared test data helpers ─────────────────────────────────────────────

    private static User MakeUser(Guid? id = null) => new()
    {
        Id         = id ?? Guid.NewGuid(),
        Email      = "user@example.com",
        FirstName  = "Test",
        LastName   = "User",
        IsActive   = true,
        IsDeleted  = false,
        FullMobile = "+447700900001"
    };

    private static Program MakeProgram(Guid? id = null, ProgramStatus status = ProgramStatus.Published) => new()
    {
        Id        = id ?? Guid.NewGuid(),
        Name      = "Test Program",
        Status    = status,
        IsDeleted = false
    };

    private static ProgramDuration MakeDuration(Guid programId, Guid? id = null) => new()
    {
        Id        = id ?? Guid.NewGuid(),
        ProgramId = programId,
        Label     = "6 weeks",
        Weeks     = 6,
        IsActive  = true
    };

    private static DurationPrice MakePrice(Guid durationId, Guid? id = null) => new()
    {
        Id             = id ?? Guid.NewGuid(),
        DurationId     = durationId,
        LocationCode   = "GB",
        Amount         = 320m,
        CurrencyCode   = "GBP",
        CurrencySymbol = "£",
        IsActive       = true
    };

    private void SetupHappyPath(User user, Program program, ProgramDuration duration, DurationPrice price)
    {
        _orders.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync((Order?)null);   // no existing order for idempotency

        _users.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        _durations.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProgramDuration, bool>>>(),
                It.IsAny<CancellationToken>()))
                  .ReturnsAsync(duration);

        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);

        _access.Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<UserProgramAccess, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);

        _prices.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<DurationPrice, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(price);

        _orders.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.CreateOrderAsync(It.IsAny<CreateGatewayOrderRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new GatewayCreateOrderResult("gw_123", null, "https://paypal.com/approve"));
        _gatewayFactory.Setup(f => f.GetGatewayByType(It.IsAny<PaymentGateway>()))
                       .Returns(mockGateway.Object);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_IdempotentRequest_ReturnsExistingOrderWithoutCreatingNew()
    {
        // Arrange — existing order for the idempotency key
        var existingOrder = new Order
        {
            Id             = Guid.NewGuid(),
            PaymentGateway = PaymentGateway.PayPal,
            AmountPaid     = 320m,
            CurrencyCode   = "GBP",
            GatewayOrderId = "gw_existing",
            GatewayResponse = "https://paypal.com/existing",
            IdempotencyKey = "idempotent-key-1"
        };

        _orders.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(existingOrder);

        var handler = CreateHandler();
        var cmd = new InitiateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), null, "idempotent-key-1", null, null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert — returns existing order, no new order created
        Assert.Equal(existingOrder.Id, result.OrderId);
        Assert.Equal("gw_existing", result.GatewayOrderId);
        _orders.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PublishedProgramWithPrice_CreatesOrderAndReturnsResponse()
    {
        // Arrange
        var user     = MakeUser();
        var program  = MakeProgram();
        var duration = MakeDuration(program.Id);
        var price    = MakePrice(duration.Id);
        SetupHappyPath(user, program, duration, price);

        var handler = CreateHandler();
        var cmd = new InitiateOrderCommand(user.Id, duration.Id, null, Guid.NewGuid().ToString(), null, null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.OrderId);
        Assert.Equal(320m, result.Amount);
        Assert.Equal("GBP", result.Currency);
        _orders.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnpublishedProgram_ThrowsDomainException()
    {
        // Arrange
        var user     = MakeUser();
        var program  = MakeProgram(status: ProgramStatus.Draft); // not published
        var duration = MakeDuration(program.Id);
        var price    = MakePrice(duration.Id);
        SetupHappyPath(user, program, duration, price);

        var handler = CreateHandler();
        var cmd = new InitiateOrderCommand(user.Id, duration.Id, null, Guid.NewGuid().ToString(), null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("not currently available for purchase", ex.Message);
    }

    [Fact]
    public async Task Handle_DuplicateActiveEnrollment_ThrowsDomainException()
    {
        // Arrange
        var user     = MakeUser();
        var program  = MakeProgram();
        var duration = MakeDuration(program.Id);
        var price    = MakePrice(duration.Id);
        SetupHappyPath(user, program, duration, price);

        // Override: user already has an active enrollment
        _access.Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<UserProgramAccess, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        var handler = CreateHandler();
        var cmd = new InitiateOrderCommand(user.Id, duration.Id, null, Guid.NewGuid().ToString(), null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("already have an active enrollment", ex.Message);
    }

    [Fact]
    public async Task Handle_ValidPercentageCoupon_AppliesDiscountToFinalAmount()
    {
        // Arrange
        var user     = MakeUser();
        var program  = MakeProgram();
        var duration = MakeDuration(program.Id);
        var price    = MakePrice(duration.Id); // £320
        SetupHappyPath(user, program, duration, price);

        var coupon = new Coupon
        {
            Id            = Guid.NewGuid(),
            Code          = "SAVE10",
            DiscountType  = DiscountType.Percentage,
            DiscountValue = 10m,       // 10%
            IsActive      = true,
            UsedCount     = 0
        };
        _coupons.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Coupon, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(coupon);

        var handler = CreateHandler();
        var cmd = new InitiateOrderCommand(user.Id, duration.Id, "SAVE10", Guid.NewGuid().ToString(), null, null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert — 10% off £320 = £288
        Assert.Equal(288m, result.Amount);
    }
}
