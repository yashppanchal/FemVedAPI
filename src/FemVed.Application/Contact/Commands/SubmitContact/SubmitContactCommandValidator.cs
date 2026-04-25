using FluentValidation;

namespace FemVed.Application.Contact.Commands.SubmitContact;

/// <summary>Validates <see cref="SubmitContactCommand"/>.</summary>
public sealed class SubmitContactCommandValidator : AbstractValidator<SubmitContactCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public SubmitContactCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(254).WithMessage("Email must not exceed 254 characters.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(5000).WithMessage("Message must not exceed 5000 characters.");
    }
}
