using FluentValidation;

namespace FemVed.Application.Library.Commands.RejectLibraryVideo;

/// <summary>Validates <see cref="RejectLibraryVideoCommand"/>.</summary>
public sealed class RejectLibraryVideoCommandValidator : AbstractValidator<RejectLibraryVideoCommand>
{
    /// <summary>Initialises validation rules.</summary>
    public RejectLibraryVideoCommandValidator()
    {
        RuleFor(x => x.VideoId).NotEmpty().WithMessage("VideoId is required.");
    }
}
