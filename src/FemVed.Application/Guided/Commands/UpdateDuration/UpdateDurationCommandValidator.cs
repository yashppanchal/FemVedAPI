using FluentValidation;

namespace FemVed.Application.Guided.Commands.UpdateDuration;

/// <summary>Validates <see cref="UpdateDurationCommand"/> inputs.</summary>
public sealed class UpdateDurationCommandValidator : AbstractValidator<UpdateDurationCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateDurationCommandValidator()
    {
        RuleFor(x => x.DurationId)
            .NotEmpty().WithMessage("DurationId is required.");

        RuleFor(x => x.ProgramId)
            .NotEmpty().WithMessage("ProgramId is required.");

        RuleFor(x => x.Label)
            .MaximumLength(100).WithMessage("Label must not exceed 100 characters.")
            .When(x => x.Label is not null);

        RuleFor(x => x.Weeks)
            .GreaterThan((short)0).WithMessage("Weeks must be greater than 0.")
            .When(x => x.Weeks.HasValue);
    }
}
