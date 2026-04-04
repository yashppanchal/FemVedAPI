using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateLibraryTestimonial;

/// <summary>Validates <see cref="UpdateLibraryTestimonialCommand"/>.</summary>
public sealed class UpdateLibraryTestimonialCommandValidator : AbstractValidator<UpdateLibraryTestimonialCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateLibraryTestimonialCommandValidator()
    {
        RuleFor(x => x.TestimonialId).NotEmpty();
    }
}
