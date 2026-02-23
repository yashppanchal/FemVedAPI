using FemVed.Domain.Entities;

namespace FemVed.Application.Interfaces;

/// <summary>Generates and validates JWT access tokens and refresh tokens.</summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a signed JWT access token for the given user.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>Signed JWT access token string.</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a cryptographically random refresh token and returns the raw value.
    /// The caller is responsible for hashing and persisting it.
    /// </summary>
    /// <returns>Raw refresh token string (never stored — only sent to client).</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Extracts the user ID claim from a JWT without validating expiry.
    /// Used during refresh token rotation to identify the user.
    /// </summary>
    /// <param name="accessToken">The (possibly expired) access token.</param>
    /// <returns>User ID if the token signature is valid; otherwise null.</returns>
    Guid? GetUserIdFromExpiredToken(string accessToken);

    /// <summary>
    /// Generates a short-lived (24-hour) signed JWT for email address verification.
    /// The token is embedded in the verification link sent to the user.
    /// </summary>
    /// <param name="userId">The user ID to embed as the subject claim.</param>
    /// <returns>Signed email verification JWT string.</returns>
    string GenerateEmailVerificationToken(Guid userId);

    /// <summary>
    /// Validates an email verification token and returns the user ID if valid.
    /// Unlike <see cref="GetUserIdFromExpiredToken"/>, this method enforces expiry.
    /// </summary>
    /// <param name="token">The email verification JWT.</param>
    /// <returns>User ID if the token is valid and not expired; otherwise null.</returns>
    Guid? ValidateEmailVerificationToken(string token);
}
