using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetLibraryVideoEditDetails;

/// <summary>Handles <see cref="GetLibraryVideoEditDetailsQuery"/>.</summary>
public sealed class GetLibraryVideoEditDetailsQueryHandler
    : IRequestHandler<GetLibraryVideoEditDetailsQuery, AdminLibraryVideoDetail>
{
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryCategory> _categories;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<LibraryPriceTier> _tiers;
    private readonly IRepository<LibraryVideoEpisode> _episodes;
    private readonly IRepository<LibraryVideoTag> _tags;
    private readonly IRepository<LibraryVideoFeature> _features;
    private readonly IRepository<LibraryVideoTestimonial> _testimonials;
    private readonly IRepository<LibraryVideoPrice> _prices;
    private readonly ILogger<GetLibraryVideoEditDetailsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetLibraryVideoEditDetailsQueryHandler(
        IRepository<LibraryVideo> videos, IRepository<LibraryCategory> categories,
        IRepository<Expert> experts, IRepository<LibraryPriceTier> tiers,
        IRepository<LibraryVideoEpisode> episodes, IRepository<LibraryVideoTag> tags,
        IRepository<LibraryVideoFeature> features, IRepository<LibraryVideoTestimonial> testimonials,
        IRepository<LibraryVideoPrice> prices, ILogger<GetLibraryVideoEditDetailsQueryHandler> logger)
    {
        _videos = videos; _categories = categories; _experts = experts; _tiers = tiers;
        _episodes = episodes; _tags = tags; _features = features;
        _testimonials = testimonials; _prices = prices; _logger = logger;
    }

    /// <summary>Returns full video edit details.</summary>
    public async Task<AdminLibraryVideoDetail> Handle(
        GetLibraryVideoEditDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading edit details for video {VideoId}", request.VideoId);
        var video = await _videos.FirstOrDefaultAsync(v => v.Id == request.VideoId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideo), request.VideoId);

        var category = await _categories.GetByIdAsync(video.CategoryId, cancellationToken);
        var expert = await _experts.GetByIdAsync(video.ExpertId, cancellationToken);
        var tier = await _tiers.GetByIdAsync(video.PriceTierId, cancellationToken);
        var episodes = await _episodes.GetAllAsync(e => e.VideoId == video.Id, cancellationToken);
        var tags = await _tags.GetAllAsync(t => t.VideoId == video.Id, cancellationToken);
        var features = await _features.GetAllAsync(f => f.VideoId == video.Id, cancellationToken);
        var testimonials = await _testimonials.GetAllAsync(t => t.VideoId == video.Id, cancellationToken);
        var prices = await _prices.GetAllAsync(p => p.VideoId == video.Id, cancellationToken);

        return new AdminLibraryVideoDetail(
            VideoId: video.Id,
            CategoryId: video.CategoryId,
            ExpertId: video.ExpertId,
            PriceTierId: video.PriceTierId,
            Title: video.Title,
            Slug: video.Slug,
            Synopsis: video.Synopsis,
            Description: video.Description,
            CardImage: video.CardImage,
            HeroImage: video.HeroImage,
            IconEmoji: video.IconEmoji,
            GradientClass: video.GradientClass,
            TrailerUrl: video.TrailerUrl,
            StreamUrl: video.StreamUrl,
            VideoType: video.VideoType.ToString().ToUpperInvariant(),
            TotalDuration: video.TotalDuration,
            TotalDurationSeconds: video.TotalDurationSeconds,
            ReleaseYear: video.ReleaseYear,
            IsFeatured: video.IsFeatured,
            FeaturedLabel: video.FeaturedLabel,
            FeaturedPosition: video.FeaturedPosition,
            Status: video.Status.ToString().ToUpperInvariant(),
            SortOrder: video.SortOrder,
            IsDeleted: video.IsDeleted,
            CreatedAt: video.CreatedAt,
            UpdatedAt: video.UpdatedAt,
            CategoryName: category?.Name ?? "Unknown",
            ExpertName: expert?.DisplayName ?? "Unknown",
            PriceTierKey: tier?.TierKey ?? "Unknown",
            Episodes: episodes.OrderBy(e => e.SortOrder).Select(e => new AdminEpisodeDto(
                e.Id, e.EpisodeNumber, e.Title, e.Description, e.Duration,
                e.DurationSeconds, e.StreamUrl, e.ThumbnailUrl, e.IsFreePreview, e.SortOrder)).ToList(),
            Tags: tags.OrderBy(t => t.SortOrder).Select(t => new AdminVideoTagDto(t.Id, t.Tag, t.SortOrder)).ToList(),
            Features: features.OrderBy(f => f.SortOrder).Select(f => new AdminVideoFeatureDto(f.Id, f.Icon, f.Description, f.SortOrder)).ToList(),
            Testimonials: testimonials.OrderBy(t => t.SortOrder).Select(t => new AdminVideoTestimonialDto(
                t.Id, t.ReviewerName, t.ReviewText, t.Rating, t.SortOrder, t.IsActive)).ToList(),
            PriceOverrides: prices.Select(p => new AdminVideoPriceDto(
                p.Id, p.LocationCode, p.Amount, p.CurrencyCode, p.CurrencySymbol, p.OriginalAmount)).ToList());
    }
}
