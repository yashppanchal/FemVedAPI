using FemVed.Application.Auth.DTOs;
using MediatR;

namespace FemVed.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Rotates the refresh token: revokes the current token and issues a new access/refresh pair.
/// The expired access token is used only to identify the user (signature validated, expiry ignored).
/// </summary>
/// <param name="AccessToken">The (possibly expired) JWT access token.</param>
/// <param name="RefreshToken">The raw refresh token issued at last login/register/refresh.</param>
public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponse>;
