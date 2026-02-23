using MediatR;

namespace FemVed.Application.Auth.Commands.ForgotPassword;

/// <summary>
/// Initiates the password reset flow by emailing a time-limited reset link.
/// Always returns success regardless of whether the email exists (prevents enumeration).
/// </summary>
/// <param name="Email">The email address associated with the account.</param>
public record ForgotPasswordCommand(string Email) : IRequest;
