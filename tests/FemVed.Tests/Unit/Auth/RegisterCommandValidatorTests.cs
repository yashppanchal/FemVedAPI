using FluentValidation.TestHelper;
using FemVed.Application.Auth.Commands.Register;

namespace FemVed.Tests.Unit.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    // ── Email ──────────────────────────────────────────────────────────────

    [Fact]
    public void Should_Pass_When_AllFields_Valid()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Email_Empty()
    {
        var cmd = new RegisterCommand("", "Password1!", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required.");
    }

    [Fact]
    public void Should_Fail_When_Email_Invalid_Format()
    {
        var cmd = new RegisterCommand("not-an-email", "Password1!", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_When_Email_Exceeds_254_Chars()
    {
        // 246 'a' chars + "@test.com" (9) = 255 chars — one over the 254-char limit
        var longEmail = new string('a', 246) + "@test.com";
        var cmd = new RegisterCommand(longEmail, "Password1!", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email must not exceed 254 characters.");
    }

    // ── Password ───────────────────────────────────────────────────────────

    [Fact]
    public void Should_Fail_When_Password_TooShort()
    {
        var cmd = new RegisterCommand("user@example.com", "P1!", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Fact]
    public void Should_Fail_When_Password_No_Uppercase()
    {
        var cmd = new RegisterCommand("user@example.com", "password1!", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one uppercase letter.");
    }

    [Fact]
    public void Should_Fail_When_Password_No_Lowercase()
    {
        var cmd = new RegisterCommand("user@example.com", "PASSWORD1!", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one lowercase letter.");
    }

    [Fact]
    public void Should_Fail_When_Password_No_Digit()
    {
        var cmd = new RegisterCommand("user@example.com", "Password!", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one digit.");
    }

    [Fact]
    public void Should_Fail_When_Password_No_SpecialChar()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1", "Jane", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must contain at least one special character.");
    }

    // ── Names ──────────────────────────────────────────────────────────────

    [Fact]
    public void Should_Fail_When_FirstName_Empty()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "", "Doe", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("First name is required.");
    }

    [Fact]
    public void Should_Fail_When_LastName_Empty()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "", null, null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Last name is required.");
    }

    // ── Country code + Mobile cross-field rules ────────────────────────────

    [Fact]
    public void Should_Pass_When_BothCountryCode_And_Mobile_Provided()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "Doe", "+91", "9876543210");
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_CountryCode_Provided_Without_Mobile()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "Doe", "+91", null);
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MobileNumber)
              .WithErrorMessage("Mobile number is required when country code is provided.");
    }

    [Fact]
    public void Should_Fail_When_Mobile_Provided_Without_CountryCode()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "Doe", null, "9876543210");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CountryCode)
              .WithErrorMessage("Country code is required when mobile number is provided.");
    }

    [Fact]
    public void Should_Fail_When_CountryCode_InvalidFormat()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "Doe", "91", "9876543210");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CountryCode);
    }

    [Fact]
    public void Should_Fail_When_MobileNumber_Contains_Letters()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "Doe", "+91", "987abc3210");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MobileNumber);
    }

    [Fact]
    public void Should_Fail_When_MobileNumber_TooShort()
    {
        var cmd = new RegisterCommand("user@example.com", "Password1!", "Jane", "Doe", "+91", "12345");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.MobileNumber);
    }
}
