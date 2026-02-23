using FluentValidation;
using FemVed.Domain.Enums;

namespace FemVed.Application.Admin.Commands.CreateCoupon;

/// <summary>Validates <see cref="CreateCouponCommand"/>.</summary>
public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public CreateCouponCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Coupon code is required.")
            .MaximumLength(50).WithMessage("Coupon code must not exceed 50 characters.")
            .Matches("^[A-Z0-9_-]+$").WithMessage("Coupon code must be uppercase letters, digits, hyphens or underscores only.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage("Discount value must be greater than zero.");

        When(x => x.DiscountType == DiscountType.Percentage, () =>
        {
            RuleFor(x => x.DiscountValue)
                .LessThanOrEqualTo(100).WithMessage("Percentage discount cannot exceed 100.");
        });

        RuleFor(x => x.MaxUses)
            .GreaterThan(0).When(x => x.MaxUses.HasValue)
            .WithMessage("MaxUses must be greater than zero if provided.");

        RuleFor(x => x.ValidUntil)
            .GreaterThan(x => x.ValidFrom).When(x => x.ValidFrom.HasValue && x.ValidUntil.HasValue)
            .WithMessage("ValidUntil must be after ValidFrom.");

        RuleFor(x => x.ValidUntil)
            .GreaterThan(DateTimeOffset.UtcNow).When(x => x.ValidUntil.HasValue)
            .WithMessage("ValidUntil must be a future date.");
    }
}
