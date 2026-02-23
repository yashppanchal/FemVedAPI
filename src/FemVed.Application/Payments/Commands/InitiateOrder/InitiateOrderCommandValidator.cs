using FluentValidation;

namespace FemVed.Application.Payments.Commands.InitiateOrder;

/// <summary>Validates <see cref="InitiateOrderCommand"/> inputs.</summary>
public sealed class InitiateOrderCommandValidator : AbstractValidator<InitiateOrderCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public InitiateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.DurationId)
            .NotEmpty().WithMessage("DurationId is required.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required.")
            .Must(BeAValidGuid).WithMessage("IdempotencyKey must be a valid UUID.");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("CouponCode must not exceed 50 characters.")
            .When(x => x.CouponCode is not null);
    }

    private static bool BeAValidGuid(string value) =>
        Guid.TryParse(value, out _);
}
