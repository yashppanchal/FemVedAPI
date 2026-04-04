using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateLibraryTierPrice;

/// <summary>Validates <see cref="UpdateLibraryTierPriceCommand"/>.</summary>
public sealed class UpdateLibraryTierPriceCommandValidator : AbstractValidator<UpdateLibraryTierPriceCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateLibraryTierPriceCommandValidator()
    {
        RuleFor(x => x.PriceId).NotEmpty();
    }
}
