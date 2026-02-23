using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FemVed.Infrastructure.Security;

/// <summary>
/// Generates and validates JWT access tokens, refresh tokens, and email verification tokens
/// using <c>System.IdentityModel.Tokens.Jwt</c>.
/// Configuration is read from environment variables: JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE, JWT_ACCESS_EXPIRY_MINUTES.
/// </summary>
public sealed class JwtService : IJwtService
{
    private const string EmailVerifyPurposeClaim = "purpose";
    private const string EmailVerifyPurposeValue = "email_verify";

    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessExpiryMinutes;
    private readonly ILogger<JwtService> _logger;

    /// <summary>Initialises JwtService reading config from environment variables.</summary>
    /// <param name="config">Application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public JwtService(IConfiguration config, ILogger<JwtService> logger)
    {
        _secret = config["JWT_SECRET"]
            ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set.");
        _issuer = config["JWT_ISSUER"] ?? "https://api.femved.com";
        _audience = config["JWT_AUDIENCE"] ?? "https://femved.com";
        _accessExpiryMinutes = int.TryParse(config["JWT_ACCESS_EXPIRY_MINUTES"], out var m) ? m : 15;
        _logger = logger;
    }

    /// <summary>
    /// Generates a signed JWT access token containing sub, email, and role claims.
    /// </summary>
    /// <param name="user">The authenticated user (must have Role navigation loaded).</param>
    /// <returns>Signed JWT string.</returns>
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(_accessExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", user.Role?.Name ?? "User"),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically random URL-safe Base64 refresh token.
    /// The raw value is returned once and never stored — only its SHA-256 hash is persisted.
    /// </summary>
    /// <returns>Raw refresh token string.</returns>
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    /// <summary>
    /// Extracts the user ID from a JWT without validating expiry.
    /// Used during refresh token rotation where the access token may already be expired.
    /// </summary>
    /// <param name="accessToken">The (possibly expired) access token.</param>
    /// <returns>User ID if the signature is valid; otherwise null.</returns>
    public Guid? GetUserIdFromExpiredToken(string accessToken)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Intentionally ignore expiry
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(accessToken, validationParams, out _);

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract user ID from expired token");
            return null;
        }
    }

    /// <summary>
    /// Generates a short-lived (24-hour) JWT for email address verification.
    /// Includes a <c>purpose=email_verify</c> claim to distinguish from access tokens.
    /// </summary>
    /// <param name="userId">The user ID to embed as the subject claim.</param>
    /// <returns>Signed email verification JWT.</returns>
    public string GenerateEmailVerificationToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(EmailVerifyPurposeClaim, EmailVerifyPurposeValue)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates an email verification JWT fully (including expiry) and returns the user ID.
    /// Also verifies the <c>purpose=email_verify</c> claim to prevent token misuse.
    /// </summary>
    /// <param name="token">The email verification JWT.</param>
    /// <returns>User ID if valid; otherwise null.</returns>
    public Guid? ValidateEmailVerificationToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, validationParams, out _);

            // Ensure the token was issued specifically for email verification
            var purpose = principal.FindFirst(EmailVerifyPurposeClaim)?.Value;
            if (purpose != EmailVerifyPurposeValue)
                return null;

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Email verification token validation failed");
            return null;
        }
    }
}
