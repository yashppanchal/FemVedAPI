using FluentValidation;

namespace FemVed.Application.Library.Commands.CreateLibraryVideo;

/// <summary>Validates <see cref="CreateLibraryVideoCommand"/>.</summary>
public sealed class CreateLibraryVideoCommandValidator : AbstractValidator<CreateLibraryVideoCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public CreateLibraryVideoCommandValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(x => x.ExpertId)
            .NotEmpty().WithMessage("ExpertId is required.");

        RuleFor(x => x.PriceTierId)
            .NotEmpty().WithMessage("PriceTierId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(300).WithMessage("Slug must not exceed 300 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");

        RuleFor(x => x.VideoType)
            .NotEmpty().WithMessage("VideoType is required.");
    }
}
