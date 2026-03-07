using FluentValidation;

namespace FemVed.Application.Admin.Commands.CreateExpert;

/// <summary>FluentValidation rules for <see cref="CreateExpertCommand"/>.</summary>
public sealed class CreateExpertCommandValidator : AbstractValidator<CreateExpertCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public CreateExpertCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Bio)
            .NotEmpty();

        RuleFor(x => x.GridDescription)
            .NotEmpty().WithMessage("Grid description cannot be empty or whitespace.")
            .MaximumLength(500)
            .When(x => x.GridDescription is not null);

        RuleFor(x => x.CommissionRate)
            .InclusiveBetween(0m, 100m)
            .WithMessage("CommissionRate must be between 0 and 100.");

        RuleFor(x => x.YearsExperience)
            .GreaterThan((short)0)
            .When(x => x.YearsExperience.HasValue);

        RuleFor(x => x.LocationCountry)
            .MaximumLength(100)
            .When(x => x.LocationCountry is not null);

        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
