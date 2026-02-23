using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Auth.Commands.VerifyEmail;

/// <summary>
/// Handles <see cref="VerifyEmailCommand"/>.
/// Validates the email verification JWT and sets <c>IsEmailVerified = true</c> on the user.
/// </summary>
public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IRepository<User> _users;
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public VerifyEmailCommandHandler(
        IRepository<User> users,
        IUnitOfWork uow,
        IJwtService jwt,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _users = users;
        _uow = uow;
        _jwt = jwt;
        _logger = logger;
    }

    /// <summary>Verifies the email and marks the user record accordingly.</summary>
    /// <param name="request">The verify-email command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ValidationException">Thrown when the token is invalid or expired.</exception>
    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email verification attempt");

        var userId = _jwt.ValidateEmailVerificationToken(request.Token);
        if (userId is null)
        {
            _logger.LogWarning("Invalid or expired email verification token presented");
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "token", ["This verification link is invalid or has expired. Please request a new one."] }
            });
        }

        var user = await _users.GetByIdAsync(userId.Value, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId.Value);

        if (user.IsEmailVerified)
        {
            _logger.LogInformation("Email already verified for user {UserId}", user.Id);
            return; // Idempotent — already verified is fine
        }

        user.IsEmailVerified = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        _users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verified successfully for user {UserId}", user.Id);
    }
}
