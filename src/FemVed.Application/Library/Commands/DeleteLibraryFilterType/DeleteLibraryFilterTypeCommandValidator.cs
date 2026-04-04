using FluentValidation;

namespace FemVed.Application.Library.Commands.DeleteLibraryFilterType;

/// <summary>Validates <see cref="DeleteLibraryFilterTypeCommand"/>.</summary>
public sealed class DeleteLibraryFilterTypeCommandValidator : AbstractValidator<DeleteLibraryFilterTypeCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public DeleteLibraryFilterTypeCommandValidator()
    {
        RuleFor(x => x.FilterTypeId).NotEmpty();
    }
}
