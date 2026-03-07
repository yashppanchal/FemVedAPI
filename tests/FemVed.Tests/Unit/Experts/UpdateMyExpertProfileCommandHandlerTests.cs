using FemVed.Application.Experts.Commands.UpdateMyExpertProfile;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Experts;

public class UpdateMyExpertProfileCommandHandlerTests
{
    private readonly Mock<IRepository<Expert>> _experts = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateMyExpertProfileCommandHandler CreateHandler() =>
        new(_experts.Object, _uow.Object,
            NullLogger<UpdateMyExpertProfileCommandHandler>.Instance);

    private static Expert MakeExpert(Guid userId) => new()
    {
        Id                  = Guid.NewGuid(),
        UserId              = userId,
        DisplayName         = "Dr Original",
        Title               = "Original Title",
        Bio                 = "Original Bio",
        GridDescription     = "Original grid bio",
        DetailedDescription = null,
        ProfileImageUrl     = null,
        GridImageUrl        = null,
        Specialisations     = ["Hormonal Health"],
        YearsExperience     = 5,
        Credentials         = ["MBBS"],
        LocationCountry     = "GB",
        CommissionRate      = 70m,  // should never be changed by this handler
        IsActive            = true,
        IsDeleted           = false,
        CreatedAt           = DateTimeOffset.UtcNow.AddYears(-1)
    };

    [Fact]
    public async Task Handle_ValidPatch_UpdatesFieldsAndReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expert = MakeExpert(userId);

        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var cmd = new UpdateMyExpertProfileCommand(
            UserId:             userId,
            DisplayName:        "Dr Updated",
            Title:              "New Title",
            Bio:                null,   // not patching bio
            GridDescription:    null,
            DetailedDescription:null,
            ProfileImageUrl:    null,
            GridImageUrl:       null,
            Specialisations:    null,
            YearsExperience:    null,
            Credentials:        null,
            LocationCountry:    "US");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.Equal("Dr Updated", result.DisplayName);
        Assert.Equal("New Title",  result.Title);
        Assert.Equal("Original Bio", result.Bio);   // unchanged because null patch
        Assert.Equal("US", result.LocationCountry);
        _experts.Verify(r => r.Update(It.IsAny<Expert>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpertNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expert?)null);

        var handler = CreateHandler();
        var cmd = new UpdateMyExpertProfileCommand(
            UserId: Guid.NewGuid(), DisplayName: "Test",
            Title: null, Bio: null, GridDescription: null, DetailedDescription: null,
            ProfileImageUrl: null, GridImageUrl: null, Specialisations: null,
            YearsExperience: null, Credentials: null, LocationCountry: null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllNullPatches_DoesNotChangeAnyField()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expert = MakeExpert(userId);
        var originalDisplayName = expert.DisplayName;
        var originalTitle       = expert.Title;

        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var cmd = new UpdateMyExpertProfileCommand(
            UserId: userId, DisplayName: null, Title: null, Bio: null,
            GridDescription: null, DetailedDescription: null,
            ProfileImageUrl: null, GridImageUrl: null, Specialisations: null,
            YearsExperience: null, Credentials: null, LocationCountry: null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert — nothing changed
        Assert.Equal(originalDisplayName, result.DisplayName);
        Assert.Equal(originalTitle,       result.Title);
    }

    [Fact]
    public async Task Handle_CommissionRateNotInCommand_IsNeverModified()
    {
        // Arrange — verify the command does not contain CommissionRate
        var userId = Guid.NewGuid();
        var expert = MakeExpert(userId);

        _experts.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Expert, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expert);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var cmd = new UpdateMyExpertProfileCommand(
            UserId: userId, DisplayName: "Changed",
            Title: null, Bio: null, GridDescription: null, DetailedDescription: null,
            ProfileImageUrl: null, GridImageUrl: null, Specialisations: null,
            YearsExperience: null, Credentials: null, LocationCountry: null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert — CommissionRate is returned but must be the original value (70m)
        Assert.Equal(70m, result.CommissionRate);
    }
}
