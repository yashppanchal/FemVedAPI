using FluentValidation;

namespace FemVed.Application.Library.Commands.DeleteLibraryVideo;

/// <summary>Validates <see cref="DeleteLibraryVideoCommand"/>.</summary>
public sealed class DeleteLibraryVideoCommandValidator : AbstractValidator<DeleteLibraryVideoCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public DeleteLibraryVideoCommandValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId is required.");
    }
}
