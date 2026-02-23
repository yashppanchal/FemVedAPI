using FluentValidation;

namespace FemVed.Application.Admin.Commands.ProcessGdprRequest;

/// <summary>Validates <see cref="ProcessGdprRequestCommand"/>.</summary>
public sealed class ProcessGdprRequestCommandValidator : AbstractValidator<ProcessGdprRequestCommand>
{
    private static readonly string[] ValidActions = ["Complete", "Reject"];

    /// <summary>Initialises validation rules.</summary>
    public ProcessGdprRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .Must(a => ValidActions.Contains(a, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Action must be 'Complete' or 'Reject'.");

        When(x => string.Equals(x.Action, "Reject", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty().WithMessage("RejectionReason is required when rejecting a request.")
                .MaximumLength(1000).WithMessage("RejectionReason must not exceed 1000 characters.");
        });
    }
}
