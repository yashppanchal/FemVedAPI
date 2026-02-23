using FemVed.Application.Auth.DTOs;
using MediatR;

namespace FemVed.Application.Auth.Commands.Login;

/// <summary>
/// Authenticates a user with email and password.
/// Returns a new access/refresh token pair on success.
/// </summary>
/// <param name="Email">Registered email address.</param>
/// <param name="Password">Plain-text password to verify against the stored BCrypt hash.</param>
public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
