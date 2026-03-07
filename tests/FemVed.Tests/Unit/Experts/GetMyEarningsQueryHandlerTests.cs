using FemVed.Application.Experts.Queries.GetMyEarnings;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Experts;

public class GetMyEarningsQueryHandlerTests
{
    private readonly Mock<IRepository<Expert>> _experts = new();
    private readonly Mock<IRepository<Domain.Entities.Program>> _programs = new();
    private readonly Mock<IRepository<ProgramDuration>> _durations = new();
    private readonly Mock<IRepository<Order>> _orders = new();
    private readonly Mock<IRepository<ExpertPayout>> _payouts = new();

    private GetMyEarningsQueryHandler CreateHandler() =>
        new(_experts.Object, _programs.Object, _durations.Object,
            _orders.Object, _payouts.Object,
            NullLogger<GetMyEarningsQueryHandler>.Instance);

    private static Expert MakeExpert(Guid userId, decimal commissionRate = 70m) => new()
    {
        Id             = Guid.NewGuid(),
        UserId         = userId,
        DisplayName    = "Dr Expert",
        CommissionRate = commissionRate,
        IsDeleted      = false
    };

    [Fact]
    public async Task Handle_ExpertNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expert?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetMyEarningsQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsZeroEarnings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expert = MakeExpert(userId);

        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);
        _programs.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
        _durations.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProgramDuration, bool>>>(),
                It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        _orders.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync([]);
        _payouts.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ExpertPayout, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyEarningsQuery(userId), CancellationToken.None);

        // Assert
        Assert.Empty(result.TotalEarned);
        Assert.Empty(result.ExpertShare);
        Assert.Empty(result.OutstandingBalance);
        Assert.Null(result.LastPayoutAt);
    }

    [Fact]
    public async Task Handle_WithPaidOrders_ComputesShareCorrectly()
    {
        // Arrange — expert has 70% commission rate, £320 order
        var userId = Guid.NewGuid();
        var expert = MakeExpert(userId, commissionRate: 70m);

        var programId  = Guid.NewGuid();
        var durationId = Guid.NewGuid();

        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);
        _programs.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Domain.Entities.Program { Id = programId, ExpertId = expert.Id, IsDeleted = false }]);
        _durations.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProgramDuration, bool>>>(),
                It.IsAny<CancellationToken>()))
                  .ReturnsAsync([new ProgramDuration { Id = durationId, ProgramId = programId }]);
        _orders.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync([
                   new Order { DurationId = durationId, AmountPaid = 320m, CurrencyCode = "GBP", Status = OrderStatus.Paid }
               ]);
        _payouts.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ExpertPayout, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyEarningsQuery(userId), CancellationToken.None);

        // Assert
        var gbpEarned = result.TotalEarned.Single(r => r.CurrencyCode == "GBP");
        Assert.Equal(320m, gbpEarned.Amount);

        var gbpShare = result.ExpertShare.Single(r => r.CurrencyCode == "GBP");
        Assert.Equal(224m, gbpShare.Amount);  // 70% of 320

        var gbpOutstanding = result.OutstandingBalance.Single(r => r.CurrencyCode == "GBP");
        Assert.Equal(224m, gbpOutstanding.Amount);  // no payouts made yet
    }

    [Fact]
    public async Task Handle_WithPayouts_OutstandingBalanceReducedCorrectly()
    {
        // Arrange — expert earned £224 (70% of £320), already paid £100
        var userId = Guid.NewGuid();
        var expert = MakeExpert(userId, commissionRate: 70m);

        var programId  = Guid.NewGuid();
        var durationId = Guid.NewGuid();

        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);
        _programs.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync([new Domain.Entities.Program { Id = programId, ExpertId = expert.Id, IsDeleted = false }]);
        _durations.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProgramDuration, bool>>>(),
                It.IsAny<CancellationToken>()))
                  .ReturnsAsync([new ProgramDuration { Id = durationId, ProgramId = programId }]);
        _orders.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync([
                   new Order { DurationId = durationId, AmountPaid = 320m, CurrencyCode = "GBP", Status = OrderStatus.Paid }
               ]);
        _payouts.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ExpertPayout, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new ExpertPayout { ExpertId = expert.Id, Amount = 100m, CurrencyCode = "GBP", PaidAt = DateTimeOffset.UtcNow.AddDays(-10) }
                ]);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyEarningsQuery(userId), CancellationToken.None);

        // Assert
        var outstanding = result.OutstandingBalance.Single(r => r.CurrencyCode == "GBP");
        Assert.Equal(124m, outstanding.Amount);  // 224 - 100 = 124
        Assert.NotNull(result.LastPayoutAt);
    }
}
