using FluentValidation;

namespace FemVed.Application.Library.Commands.AddLibraryTierPrice;

/// <summary>Validates <see cref="AddLibraryTierPriceCommand"/>.</summary>
public sealed class AddLibraryTierPriceCommandValidator : AbstractValidator<AddLibraryTierPriceCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public AddLibraryTierPriceCommandValidator()
    {
        RuleFor(x => x.TierId).NotEmpty();
        RuleFor(x => x.LocationCode).NotEmpty().MaximumLength(5);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty();
        RuleFor(x => x.CurrencySymbol).NotEmpty();
    }
}
