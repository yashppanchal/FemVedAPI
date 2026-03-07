using FemVed.Domain.ValueObjects;
using FluentValidation;

namespace FemVed.Application.Guided.Commands.AddDuration;

/// <summary>Validates <see cref="AddDurationCommand"/> inputs.</summary>
public sealed class AddDurationCommandValidator : AbstractValidator<AddDurationCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public AddDurationCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty().WithMessage("ProgramId is required.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required.")
            .MaximumLength(100).WithMessage("Label must not exceed 100 characters.");

        RuleFor(x => x.Weeks)
            .GreaterThan((short)0).WithMessage("Weeks must be greater than 0.");

        RuleFor(x => x.Prices)
            .NotEmpty().WithMessage("At least one price is required.");

        RuleForEach(x => x.Prices).ChildRules(price =>
        {
            price.RuleFor(p => p.LocationCode)
                .NotEmpty().WithMessage("LocationCode is required.");

            price.RuleFor(p => p.Amount)
                .GreaterThan(0).WithMessage("Price amount must be greater than 0.");

            // CurrencyCode is optional — auto-resolved from LocationCode when omitted.
            // If provided, it must match the expected currency for the given location.
            price.RuleFor(p => p.CurrencyCode)
                .Must((p, code) => code is null || CurrencyInfo.IsConsistentCode(p.LocationCode, code))
                .WithMessage(p =>
                    $"CurrencyCode '{p.CurrencyCode}' does not match the expected currency for location '{p.LocationCode}'. " +
                    $"Expected: {CurrencyInfo.TryGet(p.LocationCode)?.Code ?? "unknown"}.");
        });
    }
}
