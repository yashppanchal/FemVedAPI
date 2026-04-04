using FluentValidation;

namespace FemVed.Application.Library.Commands.CreateLibraryDomain;

/// <summary>Validates <see cref="CreateLibraryDomainCommand"/> inputs.</summary>
public sealed class CreateLibraryDomainCommandValidator : AbstractValidator<CreateLibraryDomainCommand>
{
    /// <summary>Initialises all validation rules for library domain creation.</summary>
    public CreateLibraryDomainCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Domain name is required.")
            .MaximumLength(200).WithMessage("Domain name must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Slug must be lowercase letters, digits, and hyphens only.");
    }
}
