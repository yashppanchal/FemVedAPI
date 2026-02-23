using System.Security.Cryptography;
using System.Text;
using FemVed.Application.Auth.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = FemVed.Domain.Entities.RefreshToken;

namespace FemVed.Application.Auth.Commands.Login;

/// <summary>
/// Handles <see cref="LoginCommand"/>.
/// Verifies credentials, issues a new token pair.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<DomainRefreshToken> _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IConfiguration _config;
    private readonly ILogger<LoginCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public LoginCommandHandler(
        IRepository<User> users,
        IRepository<DomainRefreshToken> refreshTokens,
        IUnitOfWork uow,
        IJwtService jwt,
        IConfiguration config,
        ILogger<LoginCommandHandler> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _uow = uow;
        _jwt = jwt;
        _config = config;
        _logger = logger;
    }

    /// <summary>Executes the login flow.</summary>
    /// <param name="request">The login command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response containing tokens and user summary.</returns>
    /// <exception cref="UnauthorizedException">Thrown when credentials are invalid or account is inactive.</exception>
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email {Email}", request.Email);

        var emailLower = request.Email.Trim().ToLowerInvariant();

        var user = await _users.FirstOrDefaultAsync(
            u => u.Email == emailLower && !u.IsDeleted,
            cancellationToken);

        // Constant-time comparison guard: verify hash even if user not found to prevent timing attacks
        var dummyHash = "$2a$12$dummyhashfortimingprotectionxxxxxxxxxxxxxxxxxxxxxxxxxx";
        var passwordValid = user is not null
            && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!passwordValid)
        {
            // Run dummy verify when user not found to equalise response time
            if (user is null)
                BCrypt.Net.BCrypt.Verify(request.Password, dummyHash);

            _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user!.IsActive)
        {
            _logger.LogWarning("Login attempt on inactive account {UserId}", user.Id);
            throw new UnauthorizedException("This account has been deactivated.");
        }

        var rawRefresh = _jwt.GenerateRefreshToken();
        var refreshToken = new DomainRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(
                int.Parse(_config["JWT_REFRESH_EXPIRY_DAYS"] ?? "7")),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _refreshTokens.AddAsync(refreshToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var accessToken = _jwt.GenerateAccessToken(user);
        var expiryMinutes = int.Parse(_config["JWT_ACCESS_EXPIRY_MINUTES"] ?? "15");

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new AuthResponse(
            accessToken,
            rawRefresh,
            DateTimeOffset.UtcNow.AddMinutes(expiryMinutes),
            new AuthUserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role?.Name ?? "User"));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
