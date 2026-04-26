using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.RestoreLibraryVideo;

/// <summary>
/// Handles <see cref="RestoreLibraryVideoCommand"/>.
/// Restores an archived video to Published, and/or clears the soft-delete flag.
/// Throws when the video is in a state that does not need restoring.
/// </summary>
public sealed class RestoreLibraryVideoCommandHandler : IRequestHandler<RestoreLibraryVideoCommand>
{
    private static readonly string[] Locations =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryVideo> _videos;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RestoreLibraryVideoCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public RestoreLibraryVideoCommandHandler(
        IRepository<LibraryVideo> videos,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<RestoreLibraryVideoCommandHandler> logger)
    {
        _videos = videos;
        _uow    = uow;
        _cache  = cache;
        _logger = logger;
    }

    /// <summary>Restores the video.</summary>
    /// <exception cref="NotFoundException">Thrown when the video does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the video is neither archived nor soft-deleted.</exception>
    public async Task Handle(RestoreLibraryVideoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Restoring library video {VideoId}", request.VideoId);

        var video = await _videos.FirstOrDefaultAsync(v => v.Id == request.VideoId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideo), request.VideoId);

        var wasSoftDeleted = video.IsDeleted;
        var wasArchived    = video.Status == VideoStatus.Archived;

        if (!wasSoftDeleted && !wasArchived)
            throw new DomainException(
                $"Video is not archived or deleted (current status: {video.Status}). Nothing to restore.");

        if (wasSoftDeleted)
            video.IsDeleted = false;

        if (wasArchived)
            video.Status = VideoStatus.Published;

        video.UpdatedAt = DateTimeOffset.UtcNow;
        _videos.Update(video);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in Locations)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "Library video {VideoId} restored (wasArchived={WasArchived}, wasSoftDeleted={WasSoftDeleted})",
            request.VideoId, wasArchived, wasSoftDeleted);
    }
}
