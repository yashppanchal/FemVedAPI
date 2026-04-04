using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.AddLibraryEpisode;

/// <summary>
/// Handles <see cref="AddLibraryEpisodeCommand"/>.
/// Verifies the parent video exists, creates a <see cref="LibraryVideoEpisode"/>,
/// and evicts the library tree cache.
/// </summary>
public sealed class AddLibraryEpisodeCommandHandler : IRequestHandler<AddLibraryEpisodeCommand, Guid>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryVideoEpisode> _episodes;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AddLibraryEpisodeCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public AddLibraryEpisodeCommandHandler(
        IRepository<LibraryVideo> videos,
        IRepository<LibraryVideoEpisode> episodes,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<AddLibraryEpisodeCommandHandler> logger)
    {
        _videos   = videos;
        _episodes = episodes;
        _uow      = uow;
        _cache    = cache;
        _logger   = logger;
    }

    /// <summary>Creates the episode and evicts the library tree cache.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new episode's primary key.</returns>
    /// <exception cref="NotFoundException">Thrown when the parent video is not found.</exception>
    public async Task<Guid> Handle(AddLibraryEpisodeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AddLibraryEpisode: adding episode {EpisodeNumber} '{Title}' to video {VideoId}",
            request.EpisodeNumber, request.Title, request.VideoId);

        var videoExists = await _videos.AnyAsync(
            v => v.Id == request.VideoId && !v.IsDeleted, cancellationToken);

        if (!videoExists)
            throw new NotFoundException(nameof(LibraryVideo), request.VideoId);

        var now = DateTimeOffset.UtcNow;

        var episode = new LibraryVideoEpisode
        {
            Id              = Guid.NewGuid(),
            VideoId         = request.VideoId,
            EpisodeNumber   = request.EpisodeNumber,
            Title           = request.Title.Trim(),
            Description     = request.Description?.Trim(),
            Duration        = request.Duration?.Trim(),
            DurationSeconds = request.DurationSeconds,
            StreamUrl       = request.StreamUrl?.Trim(),
            ThumbnailUrl    = request.ThumbnailUrl?.Trim(),
            IsFreePreview   = request.IsFreePreview,
            SortOrder       = request.SortOrder,
            CreatedAt       = now,
            UpdatedAt       = now
        };

        await _episodes.AddAsync(episode);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "AddLibraryEpisode: episode {EpisodeId} added to video {VideoId}",
            episode.Id, request.VideoId);

        return episode.Id;
    }
}
