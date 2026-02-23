using FemVed.Application.Auth.Commands.Register;
using FemVed.Application.Auth.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FemVed.Tests.Unit.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IRepository<User>> _users = new();
    private readonly Mock<IRepository<RefreshToken>> _refreshTokens = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IEmailService> _email = new();

    private readonly IConfiguration _config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT_ACCESS_EXPIRY_MINUTES"] = "15",
            ["JWT_REFRESH_EXPIRY_DAYS"]   = "7",
            ["APP_BASE_URL"]              = "https://femved.com"
        })
        .Build();

    private RegisterCommandHandler CreateHandler() =>
        new(_users.Object, _refreshTokens.Object, _uow.Object,
            _jwt.Object, _email.Object, _config,
            NullLogger<RegisterCommandHandler>.Instance);

    [Fact]
    public async Task Handle_NewEmail_ReturnsAuthResponse()
    {
        // Arrange
        _users.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);

        _users.Setup(r => r.AddAsync(It.IsAny<User>()))
              .Returns(Task.CompletedTask);

        _refreshTokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
                      .Returns(Task.CompletedTask);

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var savedUser = new User
        {
            Id        = Guid.NewGuid(),
            Email     = "jane@example.com",
            FirstName = "Jane",
            LastName  = "Doe",
            RoleId    = 3,
            IsActive  = true,
            Role      = new Role { Id = 3, Name = "User" }
        };

        _users.Setup(r => r.FirstOrDefaultAsync(
                  It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                  It.IsAny<CancellationToken>()))
              .ReturnsAsync(savedUser);

        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("raw_refresh_token");
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access_token");
        _jwt.Setup(j => j.GenerateEmailVerificationToken(It.IsAny<Guid>())).Returns("verify_token");

        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                         It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var cmd = new RegisterCommand("jane@example.com", "Password1!", "Jane", "Doe", null, null);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("raw_refresh_token", result.RefreshToken);
        Assert.Equal("jane@example.com", result.User.Email);
        Assert.Equal("Jane", result.User.FirstName);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        // Arrange
        _users.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(true); // Email already exists

        var handler = CreateHandler();
        var cmd = new RegisterCommand("existing@example.com", "Password1!", "Jane", "Doe", null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FemVed.Domain.Exceptions.ValidationException>(
            () => handler.Handle(cmd, CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey("email"));
    }

    [Fact]
    public async Task Handle_WithMobileAndCountryCode_SetsFullMobile()
    {
        // Arrange
        _users.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);

        User? capturedUser = null;
        _users.Setup(r => r.AddAsync(It.IsAny<User>()))
              .Callback<User>(u => capturedUser = u)
              .Returns(Task.CompletedTask);

        _refreshTokens.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
                      .Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var savedUser = new User
        {
            Id = Guid.NewGuid(), Email = "user@example.com",
            FirstName = "Jane", LastName = "Doe",
            RoleId = 3, Role = new Role { Id = 3, Name = "User" }
        };
        _users.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(savedUser);

        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("r");
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("a");
        _jwt.Setup(j => j.GenerateEmailVerificationToken(It.IsAny<Guid>())).Returns("v");
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                         It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "Doe", "+91", "9876543210");

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedUser);
        Assert.Equal("+919876543210", capturedUser!.FullMobile);
        Assert.Equal("+91", capturedUser.CountryDialCode);
        Assert.Equal("IN", capturedUser.CountryIsoCode);
    }
}
