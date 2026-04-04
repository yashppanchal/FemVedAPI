using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateLibraryEpisode;

/// <summary>Validates <see cref="UpdateLibraryEpisodeCommand"/>.</summary>
public sealed class UpdateLibraryEpisodeCommandValidator : AbstractValidator<UpdateLibraryEpisodeCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateLibraryEpisodeCommandValidator()
    {
        RuleFor(x => x.EpisodeId)
            .NotEmpty().WithMessage("EpisodeId is required.");
    }
}
