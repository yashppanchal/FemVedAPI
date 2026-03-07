using FluentValidation;

namespace FemVed.Application.Admin.Commands.RecordExpertPayout;

/// <summary>Validates <see cref="RecordExpertPayoutCommand"/> inputs before the handler runs.</summary>
public sealed class RecordExpertPayoutCommandValidator : AbstractValidator<RecordExpertPayoutCommand>
{
    /// <summary>Initialises all validation rules for recording an expert payout.</summary>
    public RecordExpertPayoutCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payout amount must be greater than zero.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be exactly 3 characters (ISO 4217), e.g. GBP.")
            .Matches("^[A-Z]{3}$").WithMessage("Currency code must be 3 uppercase letters, e.g. GBP.");

        RuleFor(x => x.PaidAt)
            .NotEmpty().WithMessage("PaidAt timestamp is required.")
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow.AddMinutes(5))
            .WithMessage("PaidAt cannot be in the future.");

        RuleFor(x => x.PaymentReference)
            .MaximumLength(255).WithMessage("Payment reference must not exceed 255 characters.")
            .When(x => x.PaymentReference is not null);
    }
}
