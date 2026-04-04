using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetAllLibraryVideos;

/// <summary>Handles <see cref="GetAllLibraryVideosQuery"/>.</summary>
public sealed class GetAllLibraryVideosQueryHandler
    : IRequestHandler<GetAllLibraryVideosQuery, List<AdminLibraryVideoListItem>>
{
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryCategory> _categories;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<LibraryVideoEpisode> _episodes;
    private readonly IRepository<LibraryPriceTier> _tiers;
    private readonly ILogger<GetAllLibraryVideosQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllLibraryVideosQueryHandler(
        IRepository<LibraryVideo> videos, IRepository<LibraryCategory> categories,
        IRepository<Expert> experts, IRepository<LibraryVideoEpisode> episodes,
        IRepository<LibraryPriceTier> tiers, ILogger<GetAllLibraryVideosQueryHandler> logger)
    {
        _videos = videos; _categories = categories; _experts = experts;
        _episodes = episodes; _tiers = tiers; _logger = logger;
    }

    /// <summary>Returns all library videos with denormalized names.</summary>
    public async Task<List<AdminLibraryVideoListItem>> Handle(
        GetAllLibraryVideosQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading all library videos for admin");
        var videos = await _videos.GetAllAsync(null, cancellationToken);
        var categories = await _categories.GetAllAsync(null, cancellationToken);
        var experts = await _experts.GetAllAsync(null, cancellationToken);
        var allEpisodes = await _episodes.GetAllAsync(null, cancellationToken);
        var tiers = await _tiers.GetAllAsync(null, cancellationToken);

        var catMap = categories.ToDictionary(c => c.Id, c => c.Name);
        var expertMap = experts.ToDictionary(e => e.Id, e => e.DisplayName);
        var episodeCounts = allEpisodes.GroupBy(e => e.VideoId).ToDictionary(g => g.Key, g => g.Count());
        var tierMap = tiers.ToDictionary(t => t.Id, t => t.TierKey);

        var result = videos
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new AdminLibraryVideoListItem(
                VideoId: v.Id,
                Title: v.Title,
                Slug: v.Slug,
                CardImage: v.CardImage,
                VideoType: v.VideoType.ToString().ToUpperInvariant(),
                Status: v.Status.ToString().ToUpperInvariant(),
                CategoryName: catMap.GetValueOrDefault(v.CategoryId, "Unknown"),
                ExpertName: expertMap.GetValueOrDefault(v.ExpertId, "Unknown"),
                TotalDuration: v.TotalDuration,
                EpisodeCount: v.VideoType == VideoType.Series
                    ? episodeCounts.GetValueOrDefault(v.Id, 0) : null,
                PriceTierKey: tierMap.GetValueOrDefault(v.PriceTierId, "Unknown"),
                SortOrder: v.SortOrder,
                IsFeatured: v.IsFeatured,
                IsDeleted: v.IsDeleted,
                CreatedAt: v.CreatedAt,
                UpdatedAt: v.UpdatedAt))
            .ToList();

        _logger.LogInformation("Returned {Count} library videos for admin", result.Count);
        return result;
    }
}
