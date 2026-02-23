using FluentValidation;
using FemVed.Domain.Enums;

namespace FemVed.Application.Admin.Commands.UpdateCoupon;

/// <summary>Validates <see cref="UpdateCouponCommand"/>.</summary>
public sealed class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateCouponCommandValidator()
    {
        RuleFor(x => x.CouponId)
            .NotEmpty().WithMessage("CouponId is required.");

        When(x => x.Code is not null, () =>
        {
            RuleFor(x => x.Code!)
                .MaximumLength(50).WithMessage("Coupon code must not exceed 50 characters.")
                .Matches("^[A-Z0-9_-]+$").WithMessage("Coupon code must be uppercase letters, digits, hyphens or underscores only.");
        });

        When(x => x.DiscountValue.HasValue, () =>
        {
            RuleFor(x => x.DiscountValue!.Value)
                .GreaterThan(0).WithMessage("Discount value must be greater than zero.");
        });

        When(x => x.DiscountType == DiscountType.Percentage && x.DiscountValue.HasValue, () =>
        {
            RuleFor(x => x.DiscountValue!.Value)
                .LessThanOrEqualTo(100).WithMessage("Percentage discount cannot exceed 100.");
        });

        When(x => x.MaxUses.HasValue, () =>
        {
            RuleFor(x => x.MaxUses!.Value)
                .GreaterThan(0).WithMessage("MaxUses must be greater than zero if provided.");
        });

        When(x => x.ValidUntil.HasValue, () =>
        {
            RuleFor(x => x.ValidUntil!.Value)
                .GreaterThan(DateTimeOffset.UtcNow).WithMessage("ValidUntil must be a future date.");
        });
    }
}
