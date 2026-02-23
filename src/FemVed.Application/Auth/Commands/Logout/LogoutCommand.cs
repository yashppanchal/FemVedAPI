using MediatR;

namespace FemVed.Application.Auth.Commands.Logout;

/// <summary>
/// Revokes the specified refresh token for the authenticated user.
/// The access token is already short-lived and does not need server-side revocation.
/// </summary>
/// <param name="UserId">The authenticated user's ID, extracted from the JWT claim.</param>
/// <param name="RefreshToken">The raw refresh token to revoke.</param>
public record LogoutCommand(Guid UserId, string RefreshToken) : IRequest;
