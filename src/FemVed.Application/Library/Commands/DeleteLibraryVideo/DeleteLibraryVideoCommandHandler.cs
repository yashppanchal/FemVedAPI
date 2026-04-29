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

    /// <summary>
    /// Sentinel marker prefixed onto the <see cref="DomainException"/> message when the
    /// admin tries to delete a video that has at least one purchase. The frontend pattern-matches
    /// on this prefix to offer the admin an "Archive instead" path.
    /// </summary>
    public const string PurchasedBlockMarker = "VIDEO_HAS_PURCHASES";

    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<UserLibraryAccess> _access;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteLibraryVideoCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteLibraryVideoCommandHandler(
        IRepository<LibraryVideo> videos,
        IRepository<UserLibraryAccess> access,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteLibraryVideoCommandHandler> logger)
    {
        _videos = videos;
        _access = access;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Hard-deletes the library video and evicts the tree cache. Refuses to delete if any
    /// <see cref="UserLibraryAccess"/> records exist for the video — the FK is configured with
    /// CASCADE in the database, so a hard delete would silently destroy paying customers'
    /// purchase records. Admins are forced to Archive in that case.
    /// </summary>
    /// <param name="request">The delete command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the video is not found.</exception>
    /// <exception cref="DomainException">Thrown when the video has one or more purchases.</exception>
    public async Task Handle(DeleteLibraryVideoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hard-deleting library video {VideoId}", request.VideoId);

        var video = await _videos.FirstOrDefaultAsync(
            v => v.Id == request.VideoId, cancellationToken)
            ?? throw new NotFoundException("LibraryVideo", request.VideoId);

        // ── Guard: refuse hard-delete when purchases exist ───────────────────
        var purchaseRecords = await _access.GetAllAsync(
            a => a.VideoId == request.VideoId, cancellationToken);
        var purchaseCount = purchaseRecords.Count;

        if (purchaseCount > 0)
        {
            _logger.LogWarning(
                "Refusing to hard-delete library video {VideoId}: {PurchaseCount} purchase record(s) exist. Admin must Archive instead.",
                request.VideoId, purchaseCount);

            throw new DomainException(
                $"{PurchasedBlockMarker}: This video has {purchaseCount} purchase{(purchaseCount == 1 ? "" : "s")} on record. " +
                "Deleting it would destroy paying customers' access. Archive the video instead — it will be hidden from the catalog and new purchases blocked, while existing purchasers keep access from their dashboard.");
        }

        _videos.Remove(video);
        await _uow.SaveChangesAsync(cancellationToken);

        // Evict library tree cache for all known location codes
        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("Library video {VideoId} permanently deleted. Cache evicted.", request.VideoId);
    }
}
