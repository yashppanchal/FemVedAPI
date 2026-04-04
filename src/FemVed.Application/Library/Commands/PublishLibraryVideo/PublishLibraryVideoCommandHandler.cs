using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.PublishLibraryVideo;

/// <summary>Handles <see cref="PublishLibraryVideoCommand"/>.</summary>
public sealed class PublishLibraryVideoCommandHandler : IRequestHandler<PublishLibraryVideoCommand>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PublishLibraryVideoCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public PublishLibraryVideoCommandHandler(IRepository<LibraryVideo> videos, IUnitOfWork uow, IMemoryCache cache, ILogger<PublishLibraryVideoCommandHandler> logger)
    { _videos = videos; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Publishes the video.</summary>
    public async Task Handle(PublishLibraryVideoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing library video {VideoId}", request.VideoId);
        var video = await _videos.FirstOrDefaultAsync(v => v.Id == request.VideoId && !v.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideo), request.VideoId);
        if (video.Status != VideoStatus.PendingReview)
            throw new DomainException($"Video must be in PendingReview status to publish. Current: {video.Status}");
        video.Status = VideoStatus.Published;
        video.UpdatedAt = DateTimeOffset.UtcNow;
        _videos.Update(video);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
        _logger.LogInformation("Library video {VideoId} published", request.VideoId);
    }
}
