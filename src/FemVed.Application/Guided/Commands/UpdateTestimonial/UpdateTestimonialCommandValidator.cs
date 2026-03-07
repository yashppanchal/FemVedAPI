using FluentValidation;

namespace FemVed.Application.Guided.Commands.UpdateTestimonial;

/// <summary>Validates <see cref="UpdateTestimonialCommand"/> inputs.</summary>
public sealed class UpdateTestimonialCommandValidator : AbstractValidator<UpdateTestimonialCommand>
{
    /// <summary>Initialises validation rules (applied only when fields are present).</summary>
    public UpdateTestimonialCommandValidator()
    {
        RuleFor(x => x.TestimonialId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();

        RuleFor(x => x.ReviewerName)
            .NotEmpty().WithMessage("Reviewer name cannot be empty or whitespace.")
            .MaximumLength(200).WithMessage("Reviewer name must not exceed 200 characters.")
            .When(x => x.ReviewerName is not null);

        RuleFor(x => x.ReviewerTitle)
            .MaximumLength(200).WithMessage("Reviewer title must not exceed 200 characters.")
            .When(x => x.ReviewerTitle is not null);

        RuleFor(x => x.ReviewText)
            .NotEmpty().WithMessage("Review text cannot be empty or whitespace.")
            .MaximumLength(2000).WithMessage("Review text must not exceed 2000 characters.")
            .When(x => x.ReviewText is not null);

        RuleFor(x => x.Rating)
            .InclusiveBetween((short)1, (short)5)
            .WithMessage("Rating must be between 1 and 5.")
            .When(x => x.Rating.HasValue);
    }
}
