using FemVed.Application.Interfaces;
using FemVed.Application.Payments.Queries.GetMyRefunds;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Payments;

public class GetMyRefundsQueryHandlerTests
{
    private readonly Mock<IRepository<Order>> _orders = new();
    private readonly Mock<IRepository<Refund>> _refunds = new();

    private GetMyRefundsQueryHandler CreateHandler() =>
        new(_orders.Object, _refunds.Object,
            NullLogger<GetMyRefundsQueryHandler>.Instance);

    [Fact]
    public async Task Handle_UserHasNoOrders_ReturnsEmptyList()
    {
        // Arrange
        _orders.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyRefundsQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        Assert.Empty(result);
        _refunds.Verify(r => r.GetAllAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Refund, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserHasOrdersButNoRefunds_ReturnsEmptyList()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        _orders.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Order { Id = orderId, UserId = userId, CurrencyCode = "GBP" }]);

        _refunds.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Refund, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyRefundsQuery(userId), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_UserHasRefunds_ReturnsMappedDtosNewestFirst()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var older   = DateTimeOffset.UtcNow.AddDays(-5);
        var newer   = DateTimeOffset.UtcNow.AddDays(-1);

        _orders.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync([new Order { Id = orderId, UserId = userId, CurrencyCode = "GBP" }]);

        _refunds.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Refund, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new Refund
                    {
                        Id           = Guid.NewGuid(),
                        OrderId      = orderId,
                        RefundAmount = 50m,
                        Status       = RefundStatus.Completed,
                        Reason       = "Customer request",
                        CreatedAt    = older
                    },
                    new Refund
                    {
                        Id           = Guid.NewGuid(),
                        OrderId      = orderId,
                        RefundAmount = 30m,
                        Status       = RefundStatus.Pending,
                        CreatedAt    = newer
                    }
                ]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyRefundsQuery(userId), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        // Newest first
        Assert.Equal(30m,        result[0].RefundAmount);
        Assert.Equal("Pending",  result[0].Status);
        Assert.Equal(50m,        result[1].RefundAmount);
        Assert.Equal("Completed",result[1].Status);
        Assert.Equal("GBP",      result[0].CurrencyCode);
    }
}
