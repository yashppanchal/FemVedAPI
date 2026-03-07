using FluentValidation;

namespace FemVed.Application.Admin.Commands.UpdateExpert;

/// <summary>FluentValidation rules for <see cref="UpdateExpertCommand"/>.</summary>
public sealed class UpdateExpertCommandValidator : AbstractValidator<UpdateExpertCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateExpertCommandValidator()
    {
        RuleFor(x => x.ExpertId).NotEmpty();

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.Title is not null);

        RuleFor(x => x.Bio)
            .NotEmpty()
            .When(x => x.Bio is not null);

        RuleFor(x => x.GridDescription)
            .NotEmpty().WithMessage("Grid description cannot be empty or whitespace.")
            .MaximumLength(500)
            .When(x => x.GridDescription is not null);

        RuleFor(x => x.CommissionRate)
            .InclusiveBetween(0m, 100m)
            .WithMessage("CommissionRate must be between 0 and 100.")
            .When(x => x.CommissionRate.HasValue);

        RuleFor(x => x.YearsExperience)
            .GreaterThan((short)0)
            .When(x => x.YearsExperience.HasValue);

        RuleFor(x => x.LocationCountry)
            .MaximumLength(100)
            .When(x => x.LocationCountry is not null);

        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
