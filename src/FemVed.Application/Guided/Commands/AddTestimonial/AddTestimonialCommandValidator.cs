using FluentValidation;

namespace FemVed.Application.Guided.Commands.AddTestimonial;

/// <summary>Validates <see cref="AddTestimonialCommand"/> inputs.</summary>
public sealed class AddTestimonialCommandValidator : AbstractValidator<AddTestimonialCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public AddTestimonialCommandValidator()
    {
        RuleFor(x => x.ProgramId).NotEmpty();

        RuleFor(x => x.ReviewerName)
            .NotEmpty().WithMessage("Reviewer name is required.")
            .MaximumLength(200).WithMessage("Reviewer name must not exceed 200 characters.");

        RuleFor(x => x.ReviewerTitle)
            .MaximumLength(200).WithMessage("Reviewer title must not exceed 200 characters.")
            .When(x => x.ReviewerTitle is not null);

        RuleFor(x => x.ReviewText)
            .NotEmpty().WithMessage("Review text is required.")
            .MaximumLength(2000).WithMessage("Review text must not exceed 2000 characters.");

        RuleFor(x => x.Rating)
            .InclusiveBetween((short)1, (short)5)
            .WithMessage("Rating must be between 1 and 5.")
            .When(x => x.Rating.HasValue);
    }
}
