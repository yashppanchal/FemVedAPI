using FemVed.Application.Guided.Commands.AddDuration;
using FemVed.Application.Guided.Commands.CreateProgram;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Guided;

public class AddDurationCommandHandlerTests
{
    private readonly Mock<IRepository<Domain.Entities.Program>> _programs = new();
    private readonly Mock<IRepository<Expert>> _experts = new();
    private readonly Mock<IRepository<ProgramDuration>> _durations = new();
    private readonly Mock<IRepository<DurationPrice>> _prices = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private AddDurationCommandHandler CreateHandler() =>
        new(_programs.Object, _experts.Object, _durations.Object, _prices.Object,
            _uow.Object, _cache, NullLogger<AddDurationCommandHandler>.Instance);

    private static AddDurationCommand ValidCommand(Guid programId, Guid userId, bool isAdmin = false) =>
        new(programId, userId, isAdmin,
            Label: "8 weeks", Weeks: 8, SortOrder: 2,
            Prices: [new DurationPriceInput("GB", 400m, "GBP", "£")]);

    private static Domain.Entities.Program MakeProgram(Guid? expertId = null, ProgramStatus status = ProgramStatus.Draft) => new()
    {
        Id        = Guid.NewGuid(),
        Name      = "Test Program",
        Status    = status,
        ExpertId  = expertId ?? Guid.NewGuid(),
        IsDeleted = false
    };

    private static Expert MakeExpert(Guid userId, Guid? id = null) => new()
    {
        Id          = id ?? Guid.NewGuid(),
        UserId      = userId,
        DisplayName = "Dr Test",
        IsDeleted   = false
    };

    [Fact]
    public async Task Handle_Admin_AddsDurationToAnyProgram()
    {
        // Arrange
        var program = MakeProgram(status: ProgramStatus.Published);  // admin can modify published
        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);
        _durations.Setup(r => r.AddAsync(It.IsAny<ProgramDuration>())).Returns(Task.CompletedTask);
        _prices.Setup(r => r.AddAsync(It.IsAny<DurationPrice>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var cmd = ValidCommand(program.Id, Guid.NewGuid(), isAdmin: true);

        // Act
        var resultId = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);
        _durations.Verify(r => r.AddAsync(It.IsAny<ProgramDuration>()), Times.Once);
        _prices.Verify(r => r.AddAsync(It.IsAny<DurationPrice>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpertOwner_AddsDurationToDraftProgram()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expert  = MakeExpert(userId);
        var program = MakeProgram(expertId: expert.Id, status: ProgramStatus.Draft);

        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);
        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);
        _durations.Setup(r => r.AddAsync(It.IsAny<ProgramDuration>())).Returns(Task.CompletedTask);
        _prices.Setup(r => r.AddAsync(It.IsAny<DurationPrice>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var cmd = ValidCommand(program.Id, userId, isAdmin: false);

        // Act
        var resultId = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);
    }

    [Fact]
    public async Task Handle_ExpertNotOwner_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expert  = MakeExpert(userId, id: Guid.NewGuid());
        var program = MakeProgram(expertId: Guid.NewGuid(), status: ProgramStatus.Draft); // different expert owns it

        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);
        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);

        var handler = CreateHandler();
        var cmd = ValidCommand(program.Id, userId, isAdmin: false);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [InlineData(ProgramStatus.Published)]
    [InlineData(ProgramStatus.Archived)]
    public async Task Handle_ExpertOnPublishedOrArchivedProgram_ThrowsDomainException(ProgramStatus status)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expert  = MakeExpert(userId);
        var program = MakeProgram(expertId: expert.Id, status: status);

        _programs.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Entities.Program, bool>>>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(program);
        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);

        var handler = CreateHandler();
        var cmd = ValidCommand(program.Id, userId, isAdmin: false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(cmd, CancellationToken.None));

        Assert.Contains("DRAFT or PENDING_REVIEW", ex.Message);
    }
}
