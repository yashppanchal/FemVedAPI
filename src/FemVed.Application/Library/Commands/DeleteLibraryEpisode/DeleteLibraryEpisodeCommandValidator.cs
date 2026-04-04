using FluentValidation;

namespace FemVed.Application.Library.Commands.DeleteLibraryEpisode;

/// <summary>Validates <see cref="DeleteLibraryEpisodeCommand"/>.</summary>
public sealed class DeleteLibraryEpisodeCommandValidator : AbstractValidator<DeleteLibraryEpisodeCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public DeleteLibraryEpisodeCommandValidator()
    {
        RuleFor(x => x.EpisodeId)
            .NotEmpty().WithMessage("EpisodeId is required.");
    }
}
