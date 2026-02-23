using FluentValidation;

namespace FemVed.Application.Guided.Commands.UpdateProgram;

/// <summary>Validates <see cref="UpdateProgramCommand"/> inputs.</summary>
public sealed class UpdateProgramCommandValidator : AbstractValidator<UpdateProgramCommand>
{
    /// <summary>Initialises validation rules (applied only when fields are present).</summary>
    public UpdateProgramCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotEmpty().WithMessage("Program ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(300).WithMessage("Program name must not exceed 300 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.GridDescription)
            .MaximumLength(500).WithMessage("Grid description must not exceed 500 characters.")
            .When(x => x.GridDescription is not null);
    }
}
