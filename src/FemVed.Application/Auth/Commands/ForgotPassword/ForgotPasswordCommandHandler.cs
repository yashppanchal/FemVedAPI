using System.Security.Cryptography;
using System.Text;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Handles <see cref="ForgotPasswordCommand"/>.
/// Generates a hashed reset token stored in <c>password_reset_tokens</c> and emails the raw link.
/// Always completes without revealing whether the email exists.
/// </summary>
public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<PasswordResetToken> _resetTokens;
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public ForgotPasswordCommandHandler(
        IRepository<User> users,
        IRepository<PasswordResetToken> resetTokens,
        IUnitOfWork uow,
        IEmailService email,
        IConfiguration config,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _users = users;
        _resetTokens = resetTokens;
        _uow = uow;
        _email = email;
        _config = config;
        _logger = logger;
    }

    /// <summary>Initiates the password reset flow.</summary>
    /// <param name="request">The forgot-password command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for email {Email}", request.Email);

        var emailLower = request.Email.Trim().ToLowerInvariant();
        var user = await _users.FirstOrDefaultAsync(
            u => u.Email == emailLower && !u.IsDeleted && u.IsActive,
            cancellationToken);

        // Return silently if user not found — prevents email enumeration
        if (user is null)
        {
            _logger.LogInformation("Password reset requested for non-existent email (suppressed)");
            return;
        }

        // Generate cryptographically random reset token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-").Replace("/", "_").TrimEnd('='); // URL-safe Base64

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _resetTokens.AddAsync(resetToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var baseUrl = _config["APP_BASE_URL"] ?? "https://femved.com";
        var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        try
        {
            await _email.SendAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "password_reset",
                new Dictionary<string, object>
                {
                    { "first_name", user.FirstName },
                    { "reset_link", resetLink },
                    { "expiry_minutes", 60 }
                },
                cancellationToken);

            _logger.LogInformation("Password reset email dispatched for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ForgotPassword: failed to send reset email for user {UserId} — token still valid in DB", user.Id);
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
