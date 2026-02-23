using FluentValidation;

namespace FemVed.Application.Users.Commands.UpdateMyProfile;

/// <summary>Validates <see cref="UpdateMyProfileCommand"/> inputs before the handler runs.</summary>
public sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    /// <summary>Initialises all validation rules for profile updates.</summary>
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.CountryCode)
            .Matches(@"^\+\d{1,4}$").WithMessage("Country code must start with + followed by 1–4 digits (e.g. +91, +44, +1).")
            .MaximumLength(10).WithMessage("Country code must not exceed 10 characters.")
            .When(x => !string.IsNullOrEmpty(x.CountryCode));

        RuleFor(x => x.MobileNumber)
            .Matches(@"^\d+$").WithMessage("Mobile number must contain digits only.")
            .MinimumLength(7).WithMessage("Mobile number must be at least 7 digits.")
            .MaximumLength(15).WithMessage("Mobile number must not exceed 15 digits.")
            .When(x => !string.IsNullOrEmpty(x.MobileNumber));

        // Cross-field: countryCode and mobileNumber must be provided together
        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required when country code is provided.")
            .When(x => !string.IsNullOrEmpty(x.CountryCode));

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required when mobile number is provided.")
            .When(x => !string.IsNullOrEmpty(x.MobileNumber));
    }
}
