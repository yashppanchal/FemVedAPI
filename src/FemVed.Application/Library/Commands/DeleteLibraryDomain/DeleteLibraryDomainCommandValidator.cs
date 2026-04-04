using FluentValidation;

namespace FemVed.Application.Library.Commands.DeleteLibraryDomain;

/// <summary>Validates <see cref="DeleteLibraryDomainCommand"/> inputs.</summary>
public sealed class DeleteLibraryDomainCommandValidator : AbstractValidator<DeleteLibraryDomainCommand>
{
    /// <summary>Initialises all validation rules for library domain deletion.</summary>
    public DeleteLibraryDomainCommandValidator()
    {
        RuleFor(x => x.DomainId)
            .NotEmpty().WithMessage("Domain ID is required.");
    }
}
