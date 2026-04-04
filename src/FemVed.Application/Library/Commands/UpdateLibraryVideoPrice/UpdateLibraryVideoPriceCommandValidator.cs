using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateLibraryVideoPrice;

/// <summary>Validates <see cref="UpdateLibraryVideoPriceCommand"/>.</summary>
public sealed class UpdateLibraryVideoPriceCommandValidator : AbstractValidator<UpdateLibraryVideoPriceCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateLibraryVideoPriceCommandValidator()
    {
        RuleFor(x => x.PriceId).NotEmpty();
    }
}
