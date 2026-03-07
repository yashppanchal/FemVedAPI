using FluentValidation;

namespace FemVed.Application.Experts.Commands.UpdateMyExpertProfile;

/// <summary>Validates <see cref="UpdateMyExpertProfileCommand"/> inputs.</summary>
public sealed class UpdateMyExpertProfileCommandValidator : AbstractValidator<UpdateMyExpertProfileCommand>
{
    /// <summary>Initialises validation rules (applied only when fields are present).</summary>
    public UpdateMyExpertProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name cannot be empty or whitespace.")
            .MaximumLength(200).WithMessage("Display name must not exceed 200 characters.")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title cannot be empty or whitespace.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => x.Title is not null);

        RuleFor(x => x.Bio)
            .NotEmpty().WithMessage("Bio cannot be empty or whitespace.")
            .When(x => x.Bio is not null);

        RuleFor(x => x.GridDescription)
            .NotEmpty().WithMessage("Grid description cannot be empty or whitespace.")
            .MaximumLength(500).WithMessage("Grid description must not exceed 500 characters.")
            .When(x => x.GridDescription is not null);

        RuleFor(x => x.YearsExperience)
            .GreaterThan((short)0).WithMessage("Years of experience must be greater than 0.")
            .When(x => x.YearsExperience.HasValue);

        RuleFor(x => x.LocationCountry)
            .MaximumLength(100).WithMessage("Location country must not exceed 100 characters.")
            .When(x => x.LocationCountry is not null);
    }
}
