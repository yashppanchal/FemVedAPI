using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateLibraryFilterType;

/// <summary>Validates <see cref="UpdateLibraryFilterTypeCommand"/>.</summary>
public sealed class UpdateLibraryFilterTypeCommandValidator : AbstractValidator<UpdateLibraryFilterTypeCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateLibraryFilterTypeCommandValidator()
    {
        RuleFor(x => x.FilterTypeId).NotEmpty();
    }
}
