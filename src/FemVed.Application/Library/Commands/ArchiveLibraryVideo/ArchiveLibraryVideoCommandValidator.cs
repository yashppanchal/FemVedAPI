using FluentValidation;

namespace FemVed.Application.Library.Commands.ArchiveLibraryVideo;

/// <summary>Validates <see cref="ArchiveLibraryVideoCommand"/>.</summary>
public sealed class ArchiveLibraryVideoCommandValidator : AbstractValidator<ArchiveLibraryVideoCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public ArchiveLibraryVideoCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty().WithMessage("VideoId is required.");
    }
}
