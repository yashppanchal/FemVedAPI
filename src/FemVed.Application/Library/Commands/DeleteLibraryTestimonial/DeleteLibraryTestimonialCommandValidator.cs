using FluentValidation;

namespace FemVed.Application.Library.Commands.DeleteLibraryTestimonial;

/// <summary>Validates <see cref="DeleteLibraryTestimonialCommand"/>.</summary>
public sealed class DeleteLibraryTestimonialCommandValidator : AbstractValidator<DeleteLibraryTestimonialCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public DeleteLibraryTestimonialCommandValidator()
    {
        RuleFor(x => x.TestimonialId).NotEmpty();
    }
}
