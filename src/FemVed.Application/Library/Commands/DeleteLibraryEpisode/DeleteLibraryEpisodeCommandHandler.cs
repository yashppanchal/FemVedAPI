using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.DeleteLibraryEpisode;

/// <summary>
/// Handles <see cref="DeleteLibraryEpisodeCommand"/>.
/// Loads the episode, hard-removes it, and evicts the library tree cache.
/// </summary>
public sealed class DeleteLibraryEpisodeCommandHandler : IRequestHandler<DeleteLibraryEpisodeCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryVideoEpisode> _episodes;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteLibraryEpisodeCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteLibraryEpisodeCommandHandler(
        IRepository<LibraryVideoEpisode> episodes,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteLibraryEpisodeCommandHandler> logger)
    {
        _episodes = episodes;
        _uow      = uow;
        _cache    = cache;
        _logger   = logger;
    }

    /// <summary>Deletes the episode and evicts the library tree cache.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the episode is not found.</exception>
    public async Task Handle(DeleteLibraryEpisodeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteLibraryEpisode: deleting episode {EpisodeId}", request.EpisodeId);

        var episode = await _episodes.FirstOrDefaultAsync(
            e => e.Id == request.EpisodeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideoEpisode), request.EpisodeId);

        _episodes.Remove(episode);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "DeleteLibraryEpisode: episode {EpisodeId} deleted", request.EpisodeId);
    }
}
