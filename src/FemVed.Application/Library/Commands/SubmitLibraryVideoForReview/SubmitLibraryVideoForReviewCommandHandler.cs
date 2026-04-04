using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.SubmitLibraryVideoForReview;

/// <summary>Handles <see cref="SubmitLibraryVideoForReviewCommand"/>.</summary>
public sealed class SubmitLibraryVideoForReviewCommandHandler : IRequestHandler<SubmitLibraryVideoForReviewCommand>
{
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SubmitLibraryVideoForReviewCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public SubmitLibraryVideoForReviewCommandHandler(IRepository<LibraryVideo> videos, IUnitOfWork uow, ILogger<SubmitLibraryVideoForReviewCommandHandler> logger)
    { _videos = videos; _uow = uow; _logger = logger; }

    /// <summary>Submits the video for review.</summary>
    public async Task Handle(SubmitLibraryVideoForReviewCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting library video {VideoId} for review", request.VideoId);
        var video = await _videos.FirstOrDefaultAsync(v => v.Id == request.VideoId && !v.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideo), request.VideoId);
        if (video.Status != VideoStatus.Draft)
            throw new DomainException($"Video must be in Draft status to submit for review. Current: {video.Status}");
        video.Status = VideoStatus.PendingReview;
        video.UpdatedAt = DateTimeOffset.UtcNow;
        _videos.Update(video);
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Library video {VideoId} submitted for review", request.VideoId);
    }
}
