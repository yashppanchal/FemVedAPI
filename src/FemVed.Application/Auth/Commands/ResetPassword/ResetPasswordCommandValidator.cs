using FluentValidation;

namespace FemVed.Application.Auth.Commands.ResetPassword;

/// <summary>Validates <see cref="ResetPasswordCommand"/> inputs before the handler runs.</summary>
public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    /// <summary>Initialises all validation rules for password reset.</summary>
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");
    }
}
