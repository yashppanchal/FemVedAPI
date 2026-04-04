using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateLibraryDomain;

/// <summary>Validates <see cref="UpdateLibraryDomainCommand"/> inputs.</summary>
public sealed class UpdateLibraryDomainCommandValidator : AbstractValidator<UpdateLibraryDomainCommand>
{
    /// <summary>Initialises all validation rules for library domain update.</summary>
    public UpdateLibraryDomainCommandValidator()
    {
        RuleFor(x => x.DomainId)
            .NotEmpty().WithMessage("Domain ID is required.");
    }
}
