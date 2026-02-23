using FluentValidation;

namespace FemVed.Application.Auth.Commands.ForgotPassword;

/// <summary>Validates <see cref="ForgotPasswordCommand"/> inputs before the handler runs.</summary>
public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    /// <summary>Initialises all validation rules for the forgot-password request.</summary>
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}
