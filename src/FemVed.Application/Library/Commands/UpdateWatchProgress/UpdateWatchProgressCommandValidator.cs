using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateWatchProgress;

/// <summary>Validates <see cref="UpdateWatchProgressCommand"/>.</summary>
public sealed class UpdateWatchProgressCommandValidator : AbstractValidator<UpdateWatchProgressCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateWatchProgressCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId is required.");

        RuleFor(x => x.ProgressSeconds)
            .GreaterThanOrEqualTo(0).WithMessage("ProgressSeconds must be zero or positive.");
    }
}
