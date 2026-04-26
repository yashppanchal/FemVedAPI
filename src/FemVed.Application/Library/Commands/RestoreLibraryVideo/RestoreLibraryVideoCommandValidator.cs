using FluentValidation;

namespace FemVed.Application.Library.Commands.RestoreLibraryVideo;

/// <summary>Validates <see cref="RestoreLibraryVideoCommand"/>.</summary>
public sealed class RestoreLibraryVideoCommandValidator : AbstractValidator<RestoreLibraryVideoCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public RestoreLibraryVideoCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty();
    }
}
