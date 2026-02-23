using MediatR;

namespace FemVed.Application.Auth.Commands.VerifyEmail;

/// <summary>
/// Marks the user's email address as verified using the JWT token from the verification link.
/// The token is a short-lived (24-hour) signed JWT generated at registration.
/// </summary>
/// <param name="Token">The email verification JWT from the link query string.</param>
public record VerifyEmailCommand(string Token) : IRequest;
