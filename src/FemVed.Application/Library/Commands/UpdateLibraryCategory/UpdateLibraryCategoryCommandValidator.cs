using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateLibraryCategory;

/// <summary>Validates <see cref="UpdateLibraryCategoryCommand"/> inputs.</summary>
public sealed class UpdateLibraryCategoryCommandValidator : AbstractValidator<UpdateLibraryCategoryCommand>
{
    /// <summary>Initialises all validation rules for library category update.</summary>
    public UpdateLibraryCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");
    }
}
