using FluentValidation;

namespace FemVed.Application.Library.Commands.CreateLibraryCategory;

/// <summary>Validates <see cref="CreateLibraryCategoryCommand"/> inputs.</summary>
public sealed class CreateLibraryCategoryCommandValidator : AbstractValidator<CreateLibraryCategoryCommand>
{
    /// <summary>Initialises all validation rules for library category creation.</summary>
    public CreateLibraryCategoryCommandValidator()
    {
        RuleFor(x => x.DomainId)
            .NotEmpty().WithMessage("Domain ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(200).WithMessage("Category name must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Slug must be lowercase letters, digits, and hyphens only.");
    }
}
