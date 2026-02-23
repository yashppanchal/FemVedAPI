using FluentValidation;

namespace FemVed.Application.Guided.Commands.CreateProgram;

/// <summary>Validates <see cref="CreateProgramCommand"/> inputs.</summary>
public sealed class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    /// <summary>Initialises all validation rules for program creation.</summary>
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Program name is required.")
            .MaximumLength(300).WithMessage("Program name must not exceed 300 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(300).WithMessage("Slug must not exceed 300 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase letters, digits, and hyphens only.");

        RuleFor(x => x.GridDescription)
            .NotEmpty().WithMessage("Grid description is required.")
            .MaximumLength(500).WithMessage("Grid description must not exceed 500 characters.");

        RuleFor(x => x.Overview)
            .NotEmpty().WithMessage("Overview is required.");

        RuleFor(x => x.Durations)
            .NotEmpty().WithMessage("At least one duration is required.");

        RuleForEach(x => x.Durations).ChildRules(duration =>
        {
            duration.RuleFor(d => d.Label).NotEmpty().WithMessage("Duration label is required.");
            duration.RuleFor(d => d.Weeks).GreaterThan((short)0).WithMessage("Weeks must be greater than 0.");
            duration.RuleFor(d => d.Prices).NotEmpty().WithMessage("Each duration requires at least one price.");
            duration.RuleForEach(d => d.Prices).ChildRules(price =>
            {
                price.RuleFor(p => p.LocationCode).NotEmpty().WithMessage("Location code is required.");
                price.RuleFor(p => p.Amount).GreaterThan(0).WithMessage("Price amount must be greater than 0.");
                price.RuleFor(p => p.CurrencyCode).NotEmpty().WithMessage("Currency code is required.");
                price.RuleFor(p => p.CurrencySymbol).NotEmpty().WithMessage("Currency symbol is required.");
            });
        });
    }
}
