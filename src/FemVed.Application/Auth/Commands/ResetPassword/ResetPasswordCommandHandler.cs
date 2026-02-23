using System.Security.Cryptography;
using System.Text;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = FemVed.Domain.Entities.RefreshToken;

namespace FemVed.Application.Auth.Commands.ResetPassword;

/// <summary>
/// Handles <see cref="ResetPasswordCommand"/>.
/// Validates the reset token, updates the password hash, and revokes all refresh tokens.
/// </summary>
public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<PasswordResetToken> _resetTokens;
    private readonly IRepository<DomainRefreshToken> _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public ResetPasswordCommandHandler(
        IRepository<User> users,
        IRepository<PasswordResetToken> resetTokens,
        IRepository<DomainRefreshToken> refreshTokens,
        IUnitOfWork uow,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _users = users;
        _resetTokens = resetTokens;
        _refreshTokens = refreshTokens;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Executes the password reset.</summary>
    /// <param name="request">The reset-password command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ValidationException">Thrown when the token is invalid, expired, or already used.</exception>
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset attempt");

        var tokenHash = HashToken(request.Token);
        var resetToken = await _resetTokens.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash && !t.IsUsed && t.ExpiresAt > DateTimeOffset.UtcNow,
            cancellationToken);

        if (resetToken is null)
        {
            _logger.LogWarning("Invalid or expired password reset token presented");
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "token", ["This reset link is invalid or has expired. Please request a new one."] }
            });
        }

        var user = await _users.GetByIdAsync(resetToken.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), resetToken.UserId);

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTimeOffset.UtcNow;
        _users.Update(user);

        // Mark token as used
        resetToken.IsUsed = true;
        _resetTokens.Update(resetToken);

        // Revoke ALL refresh tokens (security: force re-login everywhere)
        var activeTokens = await _refreshTokens.GetAllAsync(
            t => t.UserId == user.Id && !t.IsRevoked,
            cancellationToken);

        foreach (var rt in activeTokens)
        {
            rt.IsRevoked = true;
            _refreshTokens.Update(rt);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
