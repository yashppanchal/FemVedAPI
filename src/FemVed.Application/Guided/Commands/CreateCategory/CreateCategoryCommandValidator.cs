using FluentValidation;

namespace FemVed.Application.Guided.Commands.CreateCategory;

/// <summary>Validates <see cref="CreateCategoryCommand"/> inputs.</summary>
public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    /// <summary>Initialises all validation rules for category creation.</summary>
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.DomainId)
            .NotEmpty().WithMessage("Domain ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase letters, digits, and hyphens only.");

        RuleFor(x => x.CategoryType)
            .MaximumLength(100).WithMessage("Category type must not exceed 100 characters.")
            .When(x => x.CategoryType is not null);

        RuleFor(x => x.HeroTitle)
            .MaximumLength(300).WithMessage("Hero title must not exceed 300 characters.")
            .When(x => x.HeroTitle is not null);

        // HeroSubtext, WhatsIncluded, KeyAreas — all optional display fields
    }
}
