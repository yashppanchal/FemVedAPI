using FluentValidation;

namespace FemVed.Application.Library.Commands.AddLibraryEpisode;

/// <summary>Validates <see cref="AddLibraryEpisodeCommand"/>.</summary>
public sealed class AddLibraryEpisodeCommandValidator : AbstractValidator<AddLibraryEpisodeCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public AddLibraryEpisodeCommandValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.EpisodeNumber)
            .GreaterThan(0).WithMessage("EpisodeNumber must be greater than 0.");
    }
}
