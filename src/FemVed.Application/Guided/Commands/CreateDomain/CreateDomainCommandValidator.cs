using FluentValidation;

namespace FemVed.Application.Guided.Commands.CreateDomain;

/// <summary>Validates <see cref="CreateDomainCommand"/> inputs.</summary>
public sealed class CreateDomainCommandValidator : AbstractValidator<CreateDomainCommand>
{
    /// <summary>Initialises all validation rules for domain creation.</summary>
    public CreateDomainCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Domain name is required.")
            .MaximumLength(200).WithMessage("Domain name must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase letters, digits, and hyphens only.");
    }
}
