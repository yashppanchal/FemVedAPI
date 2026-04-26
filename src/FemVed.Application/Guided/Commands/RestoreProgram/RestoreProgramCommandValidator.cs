using FluentValidation;

namespace FemVed.Application.Guided.Commands.RestoreProgram;

/// <summary>Validates <see cref="RestoreProgramCommand"/>.</summary>
public sealed class RestoreProgramCommandValidator : AbstractValidator<RestoreProgramCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public RestoreProgramCommandValidator()
    {
        RuleFor(x => x.ProgramId).NotEmpty();
    }
}
