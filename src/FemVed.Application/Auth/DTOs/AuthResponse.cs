namespace FemVed.Application.Auth.DTOs;

/// <summary>
/// Returned by Register, Login, and RefreshToken endpoints.
/// Contains both tokens and a minimal user summary.
/// </summary>
/// <param name="AccessToken">Signed JWT access token (15-minute lifetime).</param>
/// <param name="RefreshToken">Raw refresh token to exchange for a new pair (7-day lifetime).</param>
/// <param name="AccessTokenExpiresAt">UTC expiry of the access token.</param>
/// <param name="User">Minimal user information for the client.</param>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    AuthUserDto User);

/// <summary>
/// Minimal user data included in every auth response.
/// Never includes password hash, tokens, or sensitive fields.
/// </summary>
/// <param name="Id">User primary key.</param>
/// <param name="Email">User email address.</param>
/// <param name="FirstName">User first name.</param>
/// <param name="LastName">User last name.</param>
/// <param name="Role">Numeric role ID — 1 = Admin, 2 = Expert, 3 = User.</param>
public record AuthUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    int Role);
