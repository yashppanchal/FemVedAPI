using FluentValidation;

namespace FemVed.Application.Library.Commands.PublishLibraryVideo;

/// <summary>Validates <see cref="PublishLibraryVideoCommand"/>.</summary>
public sealed class PublishLibraryVideoCommandValidator : AbstractValidator<PublishLibraryVideoCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public PublishLibraryVideoCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty().WithMessage("VideoId is required.");
    }
}
