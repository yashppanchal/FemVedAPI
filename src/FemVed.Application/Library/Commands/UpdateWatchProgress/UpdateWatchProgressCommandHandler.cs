using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateWatchProgress;

/// <summary>
/// Handles <see cref="UpdateWatchProgressCommand"/>.
/// Updates watch progress for a purchased library video (Masterclass or Series episode).
/// </summary>
public sealed class UpdateWatchProgressCommandHandler
    : IRequestHandler<UpdateWatchProgressCommand>
{
    private readonly IRepository<UserLibraryAccess> _access;
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryVideoEpisode> _episodes;
    private readonly IRepository<UserEpisodeProgress> _episodeProgress;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateWatchProgressCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateWatchProgressCommandHandler(
        IRepository<UserLibraryAccess> access,
        IRepository<LibraryVideo> videos,
        IRepository<LibraryVideoEpisode> episodes,
        IRepository<UserEpisodeProgress> episodeProgress,
        IUnitOfWork uow,
        ILogger<UpdateWatchProgressCommandHandler> logger)
    {
        _access = access;
        _videos = videos;
        _episodes = episodes;
        _episodeProgress = episodeProgress;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Updates watch progress for the video or episode.</summary>
    /// <param name="request">The command with progress data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="DomainException">Thrown when the user has not purchased this video.</exception>
    /// <exception cref="DomainException">Thrown when EpisodeId is required but missing, or episode not found.</exception>
    public async Task Handle(
        UpdateWatchProgressCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating watch progress for user {UserId}, video {VideoId}, episode {EpisodeId}, seconds {Seconds}",
            request.UserId, request.VideoId, request.EpisodeId, request.ProgressSeconds);

        // Verify purchase access
        var accessRecord = await _access.FirstOrDefaultAsync(
            a => a.UserId == request.UserId
              && a.VideoId == request.VideoId
              && a.IsActive,
            cancellationToken);

        if (accessRecord is null)
            throw new DomainException("You must purchase this video before updating watch progress.");

        // Load the video to determine type
        var video = await _videos.FirstOrDefaultAsync(
            v => v.Id == request.VideoId && !v.IsDeleted,
            cancellationToken);

        if (video is null)
            throw new NotFoundException("LibraryVideo", request.VideoId.ToString());

        var now = DateTimeOffset.UtcNow;

        if (video.VideoType == VideoType.Masterclass)
        {
            // Masterclass: update overall progress directly
            accessRecord.WatchProgressSeconds = request.ProgressSeconds;
            accessRecord.LastWatchedAt = now;
            _access.Update(accessRecord);
        }
        else
        {
            // Series: update per-episode progress, then recalculate overall
            if (!request.EpisodeId.HasValue)
                throw new DomainException("EpisodeId is required for Series-type videos.");

            // Verify episode belongs to this video
            var episode = await _episodes.FirstOrDefaultAsync(
                e => e.Id == request.EpisodeId.Value && e.VideoId == video.Id,
                cancellationToken);
            if (episode is null)
                throw new NotFoundException("LibraryVideoEpisode", request.EpisodeId.Value.ToString());

            // Upsert episode progress
            var progress = await _episodeProgress.FirstOrDefaultAsync(
                p => p.UserId == request.UserId && p.EpisodeId == request.EpisodeId.Value,
                cancellationToken);

            if (progress is null)
            {
                progress = new UserEpisodeProgress
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    EpisodeId = request.EpisodeId.Value,
                    WatchProgressSeconds = request.ProgressSeconds,
                    IsCompleted = episode.DurationSeconds.HasValue
                        && request.ProgressSeconds >= episode.DurationSeconds.Value * 0.9,
                    LastWatchedAt = now
                };
                await _episodeProgress.AddAsync(progress);
            }
            else
            {
                progress.WatchProgressSeconds = request.ProgressSeconds;
                progress.IsCompleted = episode.DurationSeconds.HasValue
                    && request.ProgressSeconds >= episode.DurationSeconds.Value * 0.9;
                progress.LastWatchedAt = now;
                _episodeProgress.Update(progress);
            }

            // Recalculate overall progress from all episodes in this video
            var videoEpisodes = await _episodes.GetAllAsync(
                e => e.VideoId == video.Id, cancellationToken);
            var episodeIds = videoEpisodes.Select(e => e.Id).ToList();
            var allProgress = await _episodeProgress.GetAllAsync(
                p => p.UserId == request.UserId && episodeIds.Contains(p.EpisodeId),
                cancellationToken);

            accessRecord.WatchProgressSeconds = allProgress.Sum(p => p.WatchProgressSeconds);
            accessRecord.LastWatchedAt = now;
            _access.Update(accessRecord);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Watch progress updated for user {UserId}, video {VideoId}",
            request.UserId, request.VideoId);
    }
}
