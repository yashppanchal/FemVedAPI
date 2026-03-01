using FluentValidation;

namespace FemVed.Application.Guided.Commands.UpdateDurationPrice;

/// <summary>Validates <see cref="UpdateDurationPriceCommand"/> inputs.</summary>
public sealed class UpdateDurationPriceCommandValidator : AbstractValidator<UpdateDurationPriceCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateDurationPriceCommandValidator()
    {
        RuleFor(x => x.PriceId)
            .NotEmpty().WithMessage("PriceId is required.");

        RuleFor(x => x.DurationId)
            .NotEmpty().WithMessage("DurationId is required.");

        RuleFor(x => x.ProgramId)
            .NotEmpty().WithMessage("ProgramId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.")
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.CurrencyCode)
            .Length(3).WithMessage("CurrencyCode must be a 3-letter ISO 4217 code, e.g. 'GBP'.")
            .When(x => x.CurrencyCode is not null);

        RuleFor(x => x.CurrencySymbol)
            .NotEmpty().WithMessage("CurrencySymbol must not be empty.")
            .When(x => x.CurrencySymbol is not null);
    }
}
