using FemVed.Application.Guided.Commands.PublishProgram;
using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Guided;

public class PublishProgramCommandHandlerTests
{
    private readonly Mock<IRepository<Domain.Entities.Program>> _programs = new();
    private readonly Mock<IRepository<ProgramDuration>> _durations = new();
    private readonly Mock<IRepository<DurationPrice>> _prices = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private PublishProgramCommandHandler CreateHandler() =>
        new(_programs.Object, _durations.Object, _prices.Object,
            _uow.Object, _cache,
            NullLogger<PublishProgramCommandHandler>.Instance);

    private static Domain.Entities.Program MakeProgram(
        Guid? id = null,
        ProgramStatus status = ProgramStatus.PendingReview) => new()
    {
        Id        = id ?? Guid.NewGuid(),
        Name      = "Test Program",
        Status    = status,
        IsDeleted = false
    };

    [Fact]
    public async Task Handle_PendingReviewProgramWithDurationsAndPrices_PublishesSuccessfully()
    {
        // Arrange
        var program  = MakeProgram();
        var duration = new ProgramDuration { Id = Guid.NewGuid(), ProgramId = program.Id, Label = "6 weeks", IsActive = true };

        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);

        _durations.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProgramDuration, bool>>>(),
                It.IsAny<CancellationToken>()))
                  .ReturnsAsync([duration]);

        _prices.Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<DurationPrice, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new PublishProgramCommand(program.Id), CancellationToken.None);

        // Assert
        Assert.Equal(ProgramStatus.Published, program.Status);
        _programs.Verify(r => r.Update(It.Is<Domain.Entities.Program>(p => p.Status == ProgramStatus.Published)), Times.Once);
    }

    [Fact]
    public async Task Handle_DraftProgramWithDurationsAndPrices_PublishesSuccessfully()
    {
        // Arrange
        var program  = MakeProgram(status: ProgramStatus.Draft);
        var duration = new ProgramDuration { Id = Guid.NewGuid(), ProgramId = program.Id, Label = "6 weeks", IsActive = true };

        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);

        _durations.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProgramDuration, bool>>>(),
                It.IsAny<CancellationToken>()))
                  .ReturnsAsync([duration]);

        _prices.Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<DurationPrice, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(true);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new PublishProgramCommand(program.Id), CancellationToken.None);

        // Assert
        Assert.Equal(ProgramStatus.Published, program.Status);
        _programs.Verify(r => r.Update(It.Is<Domain.Entities.Program>(p => p.Status == ProgramStatus.Published)), Times.Once);
    }

    [Fact]
    public async Task Handle_ProgramNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Domain.Entities.Program?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new PublishProgramCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Theory]
    [InlineData(ProgramStatus.Published)]
    [InlineData(ProgramStatus.Archived)]
    public async Task Handle_WrongStatus_ThrowsDomainException(ProgramStatus status)
    {
        // Arrange
        var program = MakeProgram(status: status);
        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new PublishProgramCommand(program.Id), CancellationToken.None));

        Assert.Contains("PENDING_REVIEW", ex.Message);
    }

    [Fact]
    public async Task Handle_NoDurations_ThrowsDomainException()
    {
        // Arrange
        var program = MakeProgram();
        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);

        _durations.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProgramDuration, bool>>>(),
                It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);   // no durations

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new PublishProgramCommand(program.Id), CancellationToken.None));

        Assert.Contains("no active durations", ex.Message);
    }

    [Fact]
    public async Task Handle_DurationWithNoPrices_ThrowsDomainException()
    {
        // Arrange
        var program  = MakeProgram();
        var duration = new ProgramDuration { Id = Guid.NewGuid(), ProgramId = program.Id, Label = "6 weeks", IsActive = true };

        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);

        _durations.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProgramDuration, bool>>>(),
                It.IsAny<CancellationToken>()))
                  .ReturnsAsync([duration]);

        _prices.Setup(r => r.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<DurationPrice, bool>>>(),
                It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);   // no prices

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(new PublishProgramCommand(program.Id), CancellationToken.None));

        Assert.Contains("no active prices", ex.Message);
    }
}
