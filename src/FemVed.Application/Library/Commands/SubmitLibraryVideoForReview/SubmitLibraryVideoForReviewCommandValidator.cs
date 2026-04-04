using FluentValidation;

namespace FemVed.Application.Library.Commands.SubmitLibraryVideoForReview;

/// <summary>Validates <see cref="SubmitLibraryVideoForReviewCommand"/>.</summary>
public sealed class SubmitLibraryVideoForReviewCommandValidator : AbstractValidator<SubmitLibraryVideoForReviewCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public SubmitLibraryVideoForReviewCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty().WithMessage("VideoId is required.");
    }
}
