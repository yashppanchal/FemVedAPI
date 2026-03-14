using FluentValidation;

namespace FemVed.Application.Admin.Commands.ChangeUserEmail;

/// <summary>Validates <see cref="ChangeUserEmailCommand"/>.</summary>
public sealed class ChangeUserEmailCommandValidator : AbstractValidator<ChangeUserEmailCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public ChangeUserEmailCommandValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("New email is required.")
            .EmailAddress().WithMessage("New email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
    }
}
