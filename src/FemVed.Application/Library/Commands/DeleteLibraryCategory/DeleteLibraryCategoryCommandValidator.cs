using FluentValidation;

namespace FemVed.Application.Library.Commands.DeleteLibraryCategory;

/// <summary>Validates <see cref="DeleteLibraryCategoryCommand"/> inputs.</summary>
public sealed class DeleteLibraryCategoryCommandValidator : AbstractValidator<DeleteLibraryCategoryCommand>
{
    /// <summary>Initialises all validation rules for library category deletion.</summary>
    public DeleteLibraryCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");
    }
}
