using FluentValidation.TestHelper;
using FemVed.Application.Auth.Commands.Login;

namespace FemVed.Tests.Unit.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    [Fact]
    public void Should_Pass_When_Email_And_Password_Valid()
    {
        var cmd = new LoginCommand("user@example.com", "anyPassword");
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Email_Empty()
    {
        var cmd = new LoginCommand("", "anyPassword");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required.");
    }

    [Fact]
    public void Should_Fail_When_Email_Invalid_Format()
    {
        var cmd = new LoginCommand("not-valid", "anyPassword");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("A valid email address is required.");
    }

    [Fact]
    public void Should_Fail_When_Password_Empty()
    {
        var cmd = new LoginCommand("user@example.com", "");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is required.");
    }
}
