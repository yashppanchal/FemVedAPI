using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.DeleteLibraryVideo;

/// <summary>
/// Handles <see cref="DeleteLibraryVideoCommand"/>.
/// Hard-deletes the video and evicts the library tree cache.
/// </summary>
public sealed class DeleteLibraryVideoCommandHandler : IRequestHandler<DeleteLibraryVideoCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryVideo> _videos;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteLibraryVideoCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteLibraryVideoCommandHandler(
        IRepository<LibraryVideo> videos,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteLibraryVideoCommandHandler> logger)
    {
        _videos = videos;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Hard-deletes the library video and evicts the tree cache.
    /// </summary>
    /// <param name="request">The delete command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the video is not found.</exception>
    public async Task Handle(DeleteLibraryVideoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hard-deleting library video {VideoId}", request.VideoId);

        var video = await _videos.FirstOrDefaultAsync(
            v => v.Id == request.VideoId, cancellationToken)
            ?? throw new NotFoundException("LibraryVideo", request.VideoId);

        _videos.Remove(video);
        await _uow.SaveChangesAsync(cancellationToken);

        // Evict library tree cache for all known location codes
        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("Library video {VideoId} permanently deleted. Cache evicted.", request.VideoId);
    }
}
