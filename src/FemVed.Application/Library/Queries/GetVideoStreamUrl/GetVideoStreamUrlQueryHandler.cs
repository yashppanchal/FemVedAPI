using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetVideoStreamUrl;

/// <summary>
/// Handles <see cref="GetVideoStreamUrlQuery"/>.
/// Verifies purchase access, then returns stream URLs for the video.
/// </summary>
public sealed class GetVideoStreamUrlQueryHandler
    : IRequestHandler<GetVideoStreamUrlQuery, LibraryStreamResponse>
{
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryVideoEpisode> _episodes;
    private readonly IRepository<UserLibraryAccess> _access;
    private readonly IRepository<UserEpisodeProgress> _episodeProgress;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<GetVideoStreamUrlQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetVideoStreamUrlQueryHandler(
        IRepository<LibraryVideo> videos,
        IRepository<LibraryVideoEpisode> episodes,
        IRepository<UserLibraryAccess> access,
        IRepository<UserEpisodeProgress> episodeProgress,
        IUnitOfWork uow,
        ILogger<GetVideoStreamUrlQueryHandler> logger)
    {
        _videos = videos;
        _episodes = episodes;
        _access = access;
        _episodeProgress = episodeProgress;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Returns stream URLs for the requested video.</summary>
    /// <param name="request">The query containing the slug and user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream response with URLs and progress.</returns>
    /// <exception cref="NotFoundException">Thrown when the video is not found or not published.</exception>
    /// <exception cref="DomainException">Thrown when the user has not purchased this video.</exception>
    public async Task<LibraryStreamResponse> Handle(
        GetVideoStreamUrlQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching stream URLs for video slug {Slug}, user {UserId}",
            request.Slug, request.UserId);

        var video = await _videos.FirstOrDefaultAsync(
            v => v.Slug == request.Slug
              && v.Status == VideoStatus.Published
              && !v.IsDeleted,
            cancellationToken);

        if (video is null)
            throw new NotFoundException("LibraryVideo", request.Slug);

        // Check purchase access
        var accessRecord = await _access.FirstOrDefaultAsync(
            a => a.UserId == request.UserId
              && a.VideoId == video.Id
              && a.IsActive,
            cancellationToken);

        if (accessRecord is null)
            throw new DomainException("You must purchase this video before accessing stream content.");

        // Update last watched timestamp
        accessRecord.LastWatchedAt = DateTimeOffset.UtcNow;
        _access.Update(accessRecord);
        await _uow.SaveChangesAsync(cancellationToken);

        // Build episode list with progress (for Series type)
        var episodeDtos = new List<LibraryStreamEpisodeDto>();
        if (video.VideoType == VideoType.Series)
        {
            var videoEpisodes = await _episodes.GetAllAsync(
                e => e.VideoId == video.Id, cancellationToken);

            var episodeIds = videoEpisodes.Select(e => e.Id).ToList();
            var progressRecords = await _episodeProgress.GetAllAsync(
                p => p.UserId == request.UserId && episodeIds.Contains(p.EpisodeId),
                cancellationToken);

            var progressMap = progressRecords.ToDictionary(p => p.EpisodeId);

            episodeDtos = videoEpisodes
                .OrderBy(e => e.SortOrder)
                .Select(e =>
                {
                    progressMap.TryGetValue(e.Id, out var progress);
                    return new LibraryStreamEpisodeDto(
                        EpisodeId: e.Id,
                        EpisodeNumber: e.EpisodeNumber,
                        Title: e.Title,
                        StreamUrl: e.StreamUrl,
                        WatchProgressSeconds: progress?.WatchProgressSeconds ?? 0,
                        IsCompleted: progress?.IsCompleted ?? false);
                })
                .ToList();
        }

        _logger.LogInformation("Stream URLs returned for video {Slug}, user {UserId}", request.Slug, request.UserId);

        return new LibraryStreamResponse(
            VideoId: video.Id,
            VideoType: video.VideoType.ToString().ToUpperInvariant(),
            StreamUrl: video.VideoType == VideoType.Masterclass ? video.StreamUrl : null,
            Episodes: episodeDtos,
            OverallProgressSeconds: accessRecord.WatchProgressSeconds,
            LastWatchedAt: accessRecord.LastWatchedAt);
    }
}
