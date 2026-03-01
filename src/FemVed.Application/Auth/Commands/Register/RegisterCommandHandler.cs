using System.Security.Cryptography;
using System.Text;
using FemVed.Application.Auth.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using FemVed.Domain.Utilities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = FemVed.Domain.Entities.RefreshToken;

namespace FemVed.Application.Auth.Commands.Register;

/// <summary>
/// Handles <see cref="RegisterCommand"/>.
/// Creates a new User (role = User), issues tokens, and sends a verification email.
/// </summary>
public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<DomainRefreshToken> _refreshTokens;
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<RegisterCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public RegisterCommandHandler(
        IRepository<User> users,
        IRepository<DomainRefreshToken> refreshTokens,
        IUnitOfWork uow,
        IJwtService jwt,
        IEmailService email,
        IConfiguration config,
        ILogger<RegisterCommandHandler> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _uow = uow;
        _jwt = jwt;
        _email = email;
        _config = config;
        _logger = logger;
    }

    /// <summary>Executes the registration flow.</summary>
    /// <param name="request">The register command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response containing tokens and user summary.</returns>
    /// <exception cref="ValidationException">Thrown when the email is already registered.</exception>
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration attempt for email {Email}", request.Email);

        var emailLower = request.Email.Trim().ToLowerInvariant();

        var existing = await _users.AnyAsync(u => u.Email == emailLower && !u.IsDeleted, cancellationToken);
        if (existing)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "email", ["An account with this email address already exists."] }
            });

        var isoCode = DialCodeMapper.ToIsoCode(request.CountryCode);
        var fullMobile = string.IsNullOrEmpty(request.CountryCode) || string.IsNullOrEmpty(request.MobileNumber)
            ? null
            : $"{request.CountryCode}{request.MobileNumber}";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = emailLower,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            RoleId = 3, // User
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CountryDialCode = request.CountryCode?.Trim(),
            CountryIsoCode = isoCode,
            MobileNumber = request.MobileNumber?.Trim(),
            FullMobile = fullMobile,
            WhatsAppOptIn = false,
            IsActive = true,
            IsDeleted = false,
            IsEmailVerified = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _users.AddAsync(user);

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
        var accessExpiry = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);

        // Send email verification — non-blocking; failure must never abort registration
        var verifyToken = _jwt.GenerateEmailVerificationToken(user.Id);
        var baseUrl = _config["APP_BASE_URL"] ?? "https://femved.com";
        var verifyLink = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(verifyToken)}";

        try
        {
            await _email.SendAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "email_verify",
                new Dictionary<string, object>
                {
                    { "first_name", user.FirstName },
                    { "verify_link", verifyLink }
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Registration: verification email failed for user {UserId} — registration still succeeded", user.Id);
        }

        _logger.LogInformation("User {UserId} registered successfully", user.Id);

        return new AuthResponse(
            accessToken,
            rawRefresh,
            accessExpiry,
            new AuthUserDto(user.Id, user.Email, user.FirstName, user.LastName, user.RoleId));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
