using FemVed.Application.Auth.Commands.Login;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Auth;

public class LoginCommandHandlerTests
{
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IRepository<RefreshToken>> _refreshTokens = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtService> _jwt = new();

    private readonly IConfiguration _config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT_ACCESS_EXPIRY_MINUTES"] = "15",
            ["JWT_REFRESH_EXPIRY_DAYS"]   = "7"
        })
        .Build();

    private LoginCommandHandler CreateHandler() =>
        new(_users.Object, _refreshTokens.Object, _uow.Object, _jwt.Object, _config,
            NullLogger<LoginCommandHandler>.Instance);

    private static User MakeUser(bool isActive = true)
    {
        return new User
        {
            Id           = Guid.NewGuid(),
            Email        = "jane@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 4), // low factor for speed
            IsActive     = isActive,
            IsDeleted    = false,
            FirstName    = "Jane",
            LastName     = "Doe",
            RoleId       = 3,
            Role         = new Role { Id = 3, Name = "User" }
        };
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = MakeUser();
        _users.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _refreshTokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("raw_refresh");
        _jwt.Setup(j => j.GenerateAccessToken(user)).Returns("access_token");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("jane@example.com", "Password1!"), CancellationToken.None);

        // Assert
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("raw_refresh", result.RefreshToken);
        Assert.Equal("jane@example.com", result.User.Email);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsUnauthorizedException()
    {
        // Arrange — user not found
        _users.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand("nobody@example.com", "Password1!"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedException()
    {
        // Arrange — user found but wrong password
        var user = MakeUser();
        _users.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand("jane@example.com", "WrongPassword!"), CancellationToken.None));

        Assert.Contains("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = MakeUser(isActive: false);
        _users.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand("jane@example.com", "Password1!"), CancellationToken.None));

        Assert.Contains("deactivated", ex.Message);
    }
}
