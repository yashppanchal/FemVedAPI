using FluentValidation;

namespace FemVed.Application.Payments.Commands.InitiateRefund;

/// <summary>Validates <see cref="InitiateRefundCommand"/> inputs.</summary>
public sealed class InitiateRefundCommandValidator : AbstractValidator<InitiateRefundCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public InitiateRefundCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId is required.");

        RuleFor(x => x.InitiatedByUserId)
            .NotEmpty().WithMessage("InitiatedByUserId is required.");

        RuleFor(x => x.RefundAmount)
            .GreaterThan(0).WithMessage("RefundAmount must be greater than zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
