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

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required.")
            .Must(BeAValidGuid).WithMessage("IdempotencyKey must be a valid UUID.");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("CouponCode must not exceed 50 characters.")
            .When(x => x.CouponCode is not null);

        // Exactly one of DurationId (guided) or VideoId (library) must be provided
        RuleFor(x => x)
            .Must(x => x.DurationId.HasValue ^ x.VideoId.HasValue)
            .WithMessage("Exactly one of DurationId or VideoId must be provided.");

        RuleFor(x => x.DurationId)
            .NotEqual(Guid.Empty).WithMessage("DurationId must not be an empty GUID.")
            .When(x => x.DurationId.HasValue);

        RuleFor(x => x.VideoId)
            .NotEqual(Guid.Empty).WithMessage("VideoId must not be an empty GUID.")
            .When(x => x.VideoId.HasValue);
    }

    private static bool BeAValidGuid(string value) =>
        Guid.TryParse(value, out _);
}
