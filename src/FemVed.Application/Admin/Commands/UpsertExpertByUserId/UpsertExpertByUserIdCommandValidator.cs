using FluentValidation;

namespace FemVed.Application.Admin.Commands.UpsertExpertByUserId;

/// <summary>FluentValidation rules for <see cref="UpsertExpertByUserIdCommand"/>.</summary>
public sealed class UpsertExpertByUserIdCommandValidator : AbstractValidator<UpsertExpertByUserIdCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpsertExpertByUserIdCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

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

        RuleFor(x => x.YearsExperience)
            .GreaterThan((short)0)
            .When(x => x.YearsExperience.HasValue);

        RuleFor(x => x.LocationCountry)
            .MaximumLength(100)
            .When(x => x.LocationCountry is not null);

        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
