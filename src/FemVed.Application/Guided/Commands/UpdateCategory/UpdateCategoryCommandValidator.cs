using FluentValidation;

namespace FemVed.Application.Guided.Commands.UpdateCategory;

/// <summary>Validates <see cref="UpdateCategoryCommand"/> inputs.</summary>
public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    /// <summary>Initialises validation rules (applied only when the field is present).</summary>
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.CategoryType)
            .MaximumLength(100).WithMessage("Category type must not exceed 100 characters.")
            .When(x => x.CategoryType is not null);

        RuleFor(x => x.HeroTitle)
            .MaximumLength(300).WithMessage("Hero title must not exceed 300 characters.")
            .When(x => x.HeroTitle is not null);
    }
}
