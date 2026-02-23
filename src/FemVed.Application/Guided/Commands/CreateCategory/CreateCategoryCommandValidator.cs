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
            .NotEmpty().WithMessage("Category type is required.")
            .MaximumLength(100).WithMessage("Category type must not exceed 100 characters.");

        RuleFor(x => x.HeroTitle)
            .NotEmpty().WithMessage("Hero title is required.")
            .MaximumLength(300).WithMessage("Hero title must not exceed 300 characters.");

        RuleFor(x => x.HeroSubtext)
            .NotEmpty().WithMessage("Hero subtext is required.");

        RuleFor(x => x.WhatsIncluded)
            .NotNull().WithMessage("WhatsIncluded list is required.");

        RuleFor(x => x.KeyAreas)
            .NotNull().WithMessage("KeyAreas list is required.");
    }
}
