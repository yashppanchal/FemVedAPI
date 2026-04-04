using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateLibraryEpisode;

/// <summary>
/// Handles <see cref="UpdateLibraryEpisodeCommand"/>.
/// Loads the episode, applies non-null fields, and evicts the library tree cache.
/// </summary>
public sealed class UpdateLibraryEpisodeCommandHandler : IRequestHandler<UpdateLibraryEpisodeCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryVideoEpisode> _episodes;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateLibraryEpisodeCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateLibraryEpisodeCommandHandler(
        IRepository<LibraryVideoEpisode> episodes,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<UpdateLibraryEpisodeCommandHandler> logger)
    {
        _episodes = episodes;
        _uow      = uow;
        _cache    = cache;
        _logger   = logger;
    }

    /// <summary>Updates the episode and evicts the library tree cache.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the episode is not found.</exception>
    public async Task Handle(UpdateLibraryEpisodeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateLibraryEpisode: updating episode {EpisodeId}", request.EpisodeId);

        var episode = await _episodes.FirstOrDefaultAsync(
            e => e.Id == request.EpisodeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideoEpisode), request.EpisodeId);

        if (request.EpisodeNumber.HasValue)
            episode.EpisodeNumber = request.EpisodeNumber.Value;

        if (request.Title is not null)
            episode.Title = request.Title.Trim();

        if (request.Description is not null)
            episode.Description = request.Description.Trim();

        if (request.Duration is not null)
            episode.Duration = request.Duration.Trim();

        if (request.DurationSeconds.HasValue)
            episode.DurationSeconds = request.DurationSeconds.Value;

        if (request.StreamUrl is not null)
            episode.StreamUrl = request.StreamUrl.Trim();

        if (request.ThumbnailUrl is not null)
            episode.ThumbnailUrl = request.ThumbnailUrl.Trim();

        if (request.IsFreePreview.HasValue)
            episode.IsFreePreview = request.IsFreePreview.Value;

        if (request.SortOrder.HasValue)
            episode.SortOrder = request.SortOrder.Value;

        episode.UpdatedAt = DateTimeOffset.UtcNow;

        _episodes.Update(episode);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "UpdateLibraryEpisode: episode {EpisodeId} updated", request.EpisodeId);
    }
}
