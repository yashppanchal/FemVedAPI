using FluentValidation;

namespace FemVed.Application.Library.Commands.CreateLibraryFilterType;

/// <summary>Validates <see cref="CreateLibraryFilterTypeCommand"/>.</summary>
public sealed class CreateLibraryFilterTypeCommandValidator : AbstractValidator<CreateLibraryFilterTypeCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public CreateLibraryFilterTypeCommandValidator()
    {
        RuleFor(x => x.DomainId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FilterKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FilterTarget).NotEmpty();
    }
}
