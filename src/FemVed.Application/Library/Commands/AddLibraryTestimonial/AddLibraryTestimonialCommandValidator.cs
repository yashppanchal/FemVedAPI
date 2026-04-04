using FluentValidation;

namespace FemVed.Application.Library.Commands.AddLibraryTestimonial;

/// <summary>Validates <see cref="AddLibraryTestimonialCommand"/>.</summary>
public sealed class AddLibraryTestimonialCommandValidator : AbstractValidator<AddLibraryTestimonialCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public AddLibraryTestimonialCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty();
        RuleFor(x => x.ReviewerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReviewText).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}
