using FluentValidation;

namespace FemVed.Application.Library.Commands.DeleteLibraryVideoPrice;

/// <summary>Validates <see cref="DeleteLibraryVideoPriceCommand"/>.</summary>
public sealed class DeleteLibraryVideoPriceCommandValidator : AbstractValidator<DeleteLibraryVideoPriceCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public DeleteLibraryVideoPriceCommandValidator()
    {
        RuleFor(x => x.PriceId).NotEmpty();
    }
}
