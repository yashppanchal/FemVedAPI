using FluentValidation.TestHelper;
using FemVed.Application.Admin.Commands.CreateCoupon;
using FemVed.Domain.Enums;

namespace FemVed.Tests.Unit.Admin;

public class CreateCouponCommandValidatorTests
{
    private readonly CreateCouponCommandValidator _sut = new();

    private static CreateCouponCommand ValidCommand() => new(
        AdminUserId:   Guid.NewGuid(),
        IpAddress:     "127.0.0.1",
        Code:          "WELCOME20",
        DiscountType:  DiscountType.Percentage,
        DiscountValue: 20m,
        MaxUses:       null,
        ValidFrom:     null,
        ValidUntil:    null);

    [Fact]
    public void Should_Pass_When_AllFields_Valid()
    {
        var result = _sut.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_FlatDiscount_Valid()
    {
        var cmd = ValidCommand() with { DiscountType = DiscountType.Flat, DiscountValue = 50m };
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Code_Empty()
    {
        var cmd = ValidCommand() with { Code = "" };
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Code)
              .WithErrorMessage("Coupon code is required.");
    }

    [Fact]
    public void Should_Fail_When_Code_HasLowercase()
    {
        var cmd = ValidCommand() with { Code = "welcome20" };
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Should_Fail_When_Code_TooLong()
    {
        var cmd = ValidCommand() with { Code = new string('A', 51) };
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Should_Fail_When_DiscountValue_IsZero()
    {
        var cmd = ValidCommand() with { DiscountValue = 0m };
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue)
              .WithErrorMessage("Discount value must be greater than zero.");
    }

    [Fact]
    public void Should_Fail_When_Percentage_Exceeds_100()
    {
        var cmd = ValidCommand() with { DiscountType = DiscountType.Percentage, DiscountValue = 101m };
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.DiscountValue)
              .WithErrorMessage("Percentage discount cannot exceed 100.");
    }

    [Fact]
    public void Should_Pass_When_FlatDiscount_Exceeds_100()
    {
        // Flat discounts can exceed 100 (e.g. £150 off)
        var cmd = ValidCommand() with { DiscountType = DiscountType.Flat, DiscountValue = 500m };
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.DiscountValue);
    }

    [Fact]
    public void Should_Fail_When_MaxUses_IsZero()
    {
        var cmd = ValidCommand() with { MaxUses = 0 };
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MaxUses)
              .WithErrorMessage("MaxUses must be greater than zero if provided.");
    }

    [Fact]
    public void Should_Pass_When_MaxUses_IsNull()
    {
        var cmd = ValidCommand() with { MaxUses = null };
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.MaxUses);
    }

    [Fact]
    public void Should_Fail_When_ValidUntil_BeforeValidFrom()
    {
        var from  = DateTimeOffset.UtcNow.AddDays(5);
        var until = DateTimeOffset.UtcNow.AddDays(1);
        var cmd   = ValidCommand() with { ValidFrom = from, ValidUntil = until };
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ValidUntil);
    }

    [Fact]
    public void Should_Fail_When_ValidUntil_InThePast()
    {
        var cmd = ValidCommand() with { ValidUntil = DateTimeOffset.UtcNow.AddDays(-1) };
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.ValidUntil)
              .WithErrorMessage("ValidUntil must be a future date.");
    }
}
