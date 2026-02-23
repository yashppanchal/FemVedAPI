using FluentValidation;

namespace FemVed.Application.Auth.Commands.RefreshToken;

/// <summary>Validates <see cref="RefreshTokenCommand"/> inputs before the handler runs.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>Initialises all validation rules for token refresh.</summary>
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("Access token is required.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
