using FluentValidation;

namespace FemVed.Application.Guided.Commands.AddDurationPrice;

/// <summary>Validates <see cref="AddDurationPriceCommand"/> inputs.</summary>
public sealed class AddDurationPriceCommandValidator : AbstractValidator<AddDurationPriceCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public AddDurationPriceCommandValidator()
    {
        RuleFor(x => x.DurationId)
            .NotEmpty().WithMessage("DurationId is required.");

        RuleFor(x => x.ProgramId)
            .NotEmpty().WithMessage("ProgramId is required.");

        RuleFor(x => x.LocationCode)
            .NotEmpty().WithMessage("LocationCode is required.")
            .Length(2).WithMessage("LocationCode must be a 2-letter ISO country code, e.g. 'GB'.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("CurrencyCode is required.")
            .Length(3).WithMessage("CurrencyCode must be a 3-letter ISO 4217 code, e.g. 'GBP'.");

        RuleFor(x => x.CurrencySymbol)
            .NotEmpty().WithMessage("CurrencySymbol is required.");
    }
}
