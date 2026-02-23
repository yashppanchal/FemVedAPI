using FluentValidation;

namespace FemVed.Application.Experts.Commands.SendProgressUpdate;

/// <summary>Validates <see cref="SendProgressUpdateCommand"/> inputs before the handler runs.</summary>
public sealed class SendProgressUpdateCommandValidator : AbstractValidator<SendProgressUpdateCommand>
{
    /// <summary>Initialises all validation rules for progress update submission.</summary>
    public SendProgressUpdateCommandValidator()
    {
        RuleFor(x => x.AccessId)
            .NotEmpty().WithMessage("AccessId is required.");

        RuleFor(x => x.UpdateNote)
            .NotEmpty().WithMessage("Update note is required.")
            .MinimumLength(10).WithMessage("Update note must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Update note must not exceed 2000 characters.");
    }
}
