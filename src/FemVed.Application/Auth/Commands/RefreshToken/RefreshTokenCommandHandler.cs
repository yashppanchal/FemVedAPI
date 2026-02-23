using System.Security.Cryptography;
using System.Text;
using FemVed.Application.Auth.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Handles <see cref="RefreshTokenCommand"/>.
/// Validates the current refresh token, revokes it, and issues a new pair (rotation).
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Domain.Entities.RefreshToken> _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IConfiguration _config;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public RefreshTokenCommandHandler(
        IRepository<User> users,
        IRepository<Domain.Entities.RefreshToken> refreshTokens,
        IUnitOfWork uow,
        IJwtService jwt,
        IConfiguration config,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _uow = uow;
        _jwt = jwt;
        _config = config;
        _logger = logger;
    }

    /// <summary>Executes refresh token rotation.</summary>
    /// <param name="request">The refresh token command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New auth response with rotated tokens.</returns>
    /// <exception cref="UnauthorizedException">Thrown when the token is invalid, expired, or already revoked.</exception>
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refresh token rotation requested");

        var userId = _jwt.GetUserIdFromExpiredToken(request.AccessToken);
        if (userId is null)
        {
            _logger.LogWarning("Refresh failed: could not extract user ID from access token");
            throw new UnauthorizedException("Invalid access token.");
        }

        var tokenHash = HashToken(request.RefreshToken);
        var stored = await _refreshTokens.FirstOrDefaultAsync(
            t => t.UserId == userId && t.TokenHash == tokenHash,
            cancellationToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Refresh failed for user {UserId}: token not found, revoked, or expired", userId);
            throw new UnauthorizedException("Refresh token is invalid or has expired.");
        }

        // Revoke old token immediately (rotation)
        stored.IsRevoked = true;
        _refreshTokens.Update(stored);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive, cancellationToken)
            ?? throw new UnauthorizedException("Account not found or inactive.");

        var rawRefresh = _jwt.GenerateRefreshToken();
        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawRefresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(
                int.Parse(_config["JWT_REFRESH_EXPIRY_DAYS"] ?? "7")),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _refreshTokens.AddAsync(newRefreshToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var accessToken = _jwt.GenerateAccessToken(user);
        var expiryMinutes = int.Parse(_config["JWT_ACCESS_EXPIRY_MINUTES"] ?? "15");

        _logger.LogInformation("Refresh token rotated successfully for user {UserId}", user.Id);

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
