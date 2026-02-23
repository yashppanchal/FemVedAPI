using System.Security.Cryptography;
using System.Text;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = FemVed.Domain.Entities.RefreshToken;

namespace FemVed.Application.Auth.Commands.Logout;

/// <summary>
/// Handles <see cref="LogoutCommand"/>.
/// Revokes the supplied refresh token so it cannot be used for future rotation.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRepository<DomainRefreshToken> _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LogoutCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public LogoutCommandHandler(
        IRepository<DomainRefreshToken> refreshTokens,
        IUnitOfWork uow,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokens = refreshTokens;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Revokes the refresh token, completing the logout.</summary>
    /// <param name="request">The logout command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logout requested for user {UserId}", request.UserId);

        var tokenHash = HashToken(request.RefreshToken);
        var stored = await _refreshTokens.FirstOrDefaultAsync(
            t => t.UserId == request.UserId && t.TokenHash == tokenHash && !t.IsRevoked,
            cancellationToken);

        if (stored is not null)
        {
            stored.IsRevoked = true;
            _refreshTokens.Update(stored);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        // Always return success — do not reveal whether the token existed
        _logger.LogInformation("Logout completed for user {UserId}", request.UserId);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
