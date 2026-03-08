using FemVed.Domain.ValueObjects;
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
            .MaximumLength(300).WithMessage("Program name must not exceed 300 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(300).WithMessage("Slug must not exceed 300 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase letters, digits, and hyphens only.");

        RuleFor(x => x.GridDescription)
            .MaximumLength(500).WithMessage("Grid description must not exceed 500 characters.")
            .When(x => x.GridDescription is not null);

        // Overview — optional display field

        RuleForEach(x => x.DetailSections).ChildRules(section =>
        {
            section.RuleFor(s => s.Heading)
                .NotEmpty().WithMessage("Section heading is required.");
            section.RuleFor(s => s.Description)
                .NotEmpty().WithMessage("Section description is required.");
        }).When(x => x.DetailSections is { Count: > 0 });

        // Durations — optional at creation; add via POST /programs/{id}/durations
        RuleForEach(x => x.Durations).ChildRules(duration =>
        {
            duration.RuleFor(d => d.Label).NotEmpty().WithMessage("Duration label is required.");
            duration.RuleFor(d => d.Weeks).GreaterThan((short)0).WithMessage("Weeks must be greater than 0.");
            duration.RuleFor(d => d.Prices).NotEmpty().WithMessage("Each duration requires at least one price.");
            duration.RuleForEach(d => d.Prices).ChildRules(price =>
            {
                price.RuleFor(p => p.LocationCode)
                    .NotEmpty().WithMessage("Location code is required.");

                price.RuleFor(p => p.Amount)
                    .GreaterThan(0).WithMessage("Price amount must be greater than 0.");

                // CurrencyCode is optional — auto-resolved from LocationCode when omitted.
                // If provided, it must match the expected currency for the given location.
                price.RuleFor(p => p.CurrencyCode)
                    .Must((p, code) => code is null || CurrencyInfo.IsConsistentCode(p.LocationCode, code))
                    .WithMessage(p =>
                        $"CurrencyCode '{p.CurrencyCode}' does not match the expected currency for location '{p.LocationCode}'. " +
                        $"Expected: {CurrencyInfo.TryGet(p.LocationCode)?.Code ?? "unknown"}.");
            });
        });
    }
}
