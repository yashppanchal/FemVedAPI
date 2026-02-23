using MediatR;

namespace FemVed.Application.Auth.Commands.ResetPassword;

/// <summary>
/// Completes the password reset flow using the token from the email link.
/// All existing refresh tokens for the user are revoked after a successful reset.
/// </summary>
/// <param name="Token">The raw reset token from the email link query string.</param>
/// <param name="NewPassword">The new plain-text password (min 8 characters).</param>
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
