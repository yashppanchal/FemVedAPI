using FemVed.Application.Payments.Commands.CancelOrder;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Payments;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IRepository<Order>> _orders = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private CancelOrderCommandHandler CreateHandler() =>
        new(_orders.Object, _uow.Object,
            NullLogger<CancelOrderCommandHandler>.Instance);

    private static Order MakeOrder(
        Guid? userId = null,
        OrderStatus status = OrderStatus.Pending)
    {
        return new Order
        {
            Id             = Guid.NewGuid(),
            UserId         = userId ?? Guid.NewGuid(),
            Status         = status,
            CurrencyCode   = "GBP",
            LocationCode   = "GB",
            IdempotencyKey = Guid.NewGuid().ToString(),
            CreatedAt      = DateTimeOffset.UtcNow,
            UpdatedAt      = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task Handle_PendingOrder_CancelsSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order  = MakeOrder(userId: userId, status: OrderStatus.Pending);

        _orders.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new CancelOrderCommand(order.Id, userId), CancellationToken.None);

        // Assert
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        _orders.Verify(r => r.Update(It.Is<Order>(o => o.Status == OrderStatus.Cancelled)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _orders.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync((Order?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new CancelOrderCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OrderBelongsToAnotherUser_ThrowsForbiddenException()
    {
        // Arrange
        var order = MakeOrder(userId: Guid.NewGuid(), status: OrderStatus.Pending);

        _orders.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var handler = CreateHandler();

        // Act & Assert — different userId supplied
        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new CancelOrderCommand(order.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Failed)]
    [InlineData(OrderStatus.Refunded)]
    public async Task Handle_NonPendingOrder_ThrowsDomainException(OrderStatus status)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var order  = MakeOrder(userId: userId, status: status);

        _orders.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(order);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new CancelOrderCommand(order.Id, userId), CancellationToken.None));

        Assert.Contains(status.ToString(), ex.Message);
    }
}
