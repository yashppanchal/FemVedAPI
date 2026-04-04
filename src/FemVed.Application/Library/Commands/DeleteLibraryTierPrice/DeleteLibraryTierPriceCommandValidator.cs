using FluentValidation;

namespace FemVed.Application.Library.Commands.DeleteLibraryTierPrice;

/// <summary>Validates <see cref="DeleteLibraryTierPriceCommand"/>.</summary>
public sealed class DeleteLibraryTierPriceCommandValidator : AbstractValidator<DeleteLibraryTierPriceCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public DeleteLibraryTierPriceCommandValidator()
    {
        RuleFor(x => x.PriceId).NotEmpty();
    }
}
