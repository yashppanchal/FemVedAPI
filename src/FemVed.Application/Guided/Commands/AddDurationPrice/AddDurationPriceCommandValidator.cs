using FemVed.Domain.ValueObjects;
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

        // CurrencyCode is optional — auto-resolved from LocationCode when omitted.
        // If provided, it must be a 3-letter code and match the expected currency for the location.
        RuleFor(x => x.CurrencyCode)
            .Length(3).WithMessage("CurrencyCode must be a 3-letter ISO 4217 code, e.g. 'GBP'.")
            .When(x => x.CurrencyCode is not null);

        RuleFor(x => x.CurrencyCode)
            .Must((cmd, code) => code is null || CurrencyInfo.IsConsistentCode(cmd.LocationCode, code))
            .WithMessage(x =>
                $"CurrencyCode '{x.CurrencyCode}' does not match the expected currency for location '{x.LocationCode}'. " +
                $"Expected: {CurrencyInfo.TryGet(x.LocationCode)?.Code ?? "unknown"}.")
            .When(x => x.CurrencyCode is not null && x.LocationCode?.Length == 2);
    }
}
