using FluentValidation;

namespace FemVed.Application.Library.Commands.AddLibraryVideoPrice;

/// <summary>Validates <see cref="AddLibraryVideoPriceCommand"/>.</summary>
public sealed class AddLibraryVideoPriceCommandValidator : AbstractValidator<AddLibraryVideoPriceCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public AddLibraryVideoPriceCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty();
        RuleFor(x => x.LocationCode).NotEmpty().MaximumLength(5);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty();
        RuleFor(x => x.CurrencySymbol).NotEmpty();
    }
}
