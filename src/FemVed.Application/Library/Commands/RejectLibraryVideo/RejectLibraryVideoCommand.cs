using MediatR;

namespace FemVed.Application.Library.Commands.RejectLibraryVideo;

/// <summary>Transitions a library video from PendingReview back to Draft status.</summary>
/// <param name="VideoId">The video to reject.</param>
public record RejectLibraryVideoCommand(Guid VideoId) : IRequest;
