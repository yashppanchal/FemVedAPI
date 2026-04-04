using FluentValidation;

namespace FemVed.Application.Library.Commands.UpdateLibraryVideo;

/// <summary>Validates <see cref="UpdateLibraryVideoCommand"/>.</summary>
public sealed class UpdateLibraryVideoCommandValidator : AbstractValidator<UpdateLibraryVideoCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public UpdateLibraryVideoCommandValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId is required.");
    }
}
