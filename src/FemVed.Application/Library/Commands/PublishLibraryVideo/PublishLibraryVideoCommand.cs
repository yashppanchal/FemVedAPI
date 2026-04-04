using MediatR;

namespace FemVed.Application.Library.Commands.PublishLibraryVideo;

/// <summary>
/// Transitions a library video from PendingReview to Published status.
/// </summary>
/// <param name="VideoId">The video to publish.</param>
public record PublishLibraryVideoCommand(Guid VideoId) : IRequest;
