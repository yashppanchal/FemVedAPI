using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.RejectLibraryVideo;

/// <summary>Handles <see cref="RejectLibraryVideoCommand"/>.</summary>
public sealed class RejectLibraryVideoCommandHandler : IRequestHandler<RejectLibraryVideoCommand>
{
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RejectLibraryVideoCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public RejectLibraryVideoCommandHandler(IRepository<LibraryVideo> videos, IUnitOfWork uow, ILogger<RejectLibraryVideoCommandHandler> logger)
    { _videos = videos; _uow = uow; _logger = logger; }

    /// <summary>Rejects the video back to draft.</summary>
    public async Task Handle(RejectLibraryVideoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rejecting library video {VideoId}", request.VideoId);
        var video = await _videos.FirstOrDefaultAsync(v => v.Id == request.VideoId && !v.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideo), request.VideoId);
        if (video.Status != VideoStatus.PendingReview)
            throw new DomainException($"Video must be in PendingReview status to reject. Current: {video.Status}");
        video.Status = VideoStatus.Draft;
        video.UpdatedAt = DateTimeOffset.UtcNow;
        _videos.Update(video);
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Library video {VideoId} rejected to draft", request.VideoId);
    }
}
