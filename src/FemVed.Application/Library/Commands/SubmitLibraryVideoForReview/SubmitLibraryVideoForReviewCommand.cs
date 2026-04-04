using MediatR;

namespace FemVed.Application.Library.Commands.SubmitLibraryVideoForReview;

/// <summary>Transitions a library video from Draft to PendingReview status.</summary>
/// <param name="VideoId">The video to submit for review.</param>
public record SubmitLibraryVideoForReviewCommand(Guid VideoId) : IRequest;
