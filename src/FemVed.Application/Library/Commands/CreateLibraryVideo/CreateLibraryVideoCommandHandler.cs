using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.CreateLibraryVideo;

/// <summary>
/// Handles <see cref="CreateLibraryVideoCommand"/>.
/// Validates referenced entities exist, checks slug uniqueness, creates the video
/// with Draft status, creates child tag and feature records, and evicts the library tree cache.
/// </summary>
public sealed class CreateLibraryVideoCommandHandler : IRequestHandler<CreateLibraryVideoCommand, Guid>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryCategory> _categories;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<LibraryPriceTier> _priceTiers;
    private readonly IRepository<LibraryVideoTag> _tags;
    private readonly IRepository<LibraryVideoFeature> _features;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateLibraryVideoCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public CreateLibraryVideoCommandHandler(
        IRepository<LibraryVideo> videos,
        IRepository<LibraryCategory> categories,
        IRepository<Expert> experts,
        IRepository<LibraryPriceTier> priceTiers,
        IRepository<LibraryVideoTag> tags,
        IRepository<LibraryVideoFeature> features,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<CreateLibraryVideoCommandHandler> logger)
    {
        _videos = videos;
        _categories = categories;
        _experts = experts;
        _priceTiers = priceTiers;
        _tags = tags;
        _features = features;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new library video in Draft status with optional tags and features.
    /// </summary>
    /// <param name="request">The create command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created video's ID.</returns>
    /// <exception cref="NotFoundException">Thrown when category, expert, or price tier is not found.</exception>
    /// <exception cref="DomainException">Thrown when the slug is already in use or the video type is invalid.</exception>
    public async Task<Guid> Handle(CreateLibraryVideoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating library video with slug {Slug}", request.Slug);

        // Validate category exists
        var categoryExists = await _categories.AnyAsync(
            c => c.Id == request.CategoryId && c.IsActive, cancellationToken);
        if (!categoryExists)
            throw new NotFoundException("LibraryCategory", request.CategoryId);

        // Validate expert exists
        var expertExists = await _experts.AnyAsync(
            e => e.Id == request.ExpertId && !e.IsDeleted, cancellationToken);
        if (!expertExists)
            throw new NotFoundException("Expert", request.ExpertId);

        // Validate price tier exists
        var priceTierExists = await _priceTiers.AnyAsync(
            pt => pt.Id == request.PriceTierId, cancellationToken);
        if (!priceTierExists)
            throw new NotFoundException("LibraryPriceTier", request.PriceTierId);

        // Check slug uniqueness
        var slugTaken = await _videos.AnyAsync(
            v => v.Slug == request.Slug && !v.IsDeleted, cancellationToken);
        if (slugTaken)
            throw new DomainException($"A library video with slug '{request.Slug}' already exists.");

        // Parse video type
        if (!Enum.TryParse<VideoType>(request.VideoType, ignoreCase: true, out var videoType))
            throw new DomainException($"Invalid video type '{request.VideoType}'. Allowed values: {string.Join(", ", Enum.GetNames<VideoType>())}.");

        var now = DateTimeOffset.UtcNow;
        var videoId = Guid.NewGuid();

        var video = new LibraryVideo
        {
            Id = videoId,
            CategoryId = request.CategoryId,
            ExpertId = request.ExpertId,
            PriceTierId = request.PriceTierId,
            Title = request.Title,
            Slug = request.Slug,
            Synopsis = request.Synopsis,
            Description = request.Description,
            CardImage = request.CardImage,
            HeroImage = request.HeroImage,
            IconEmoji = request.IconEmoji,
            GradientClass = request.GradientClass,
            TrailerUrl = request.TrailerUrl,
            StreamUrl = request.StreamUrl,
            VideoType = videoType,
            TotalDuration = request.TotalDuration,
            TotalDurationSeconds = request.TotalDurationSeconds,
            ReleaseYear = request.ReleaseYear,
            IsFeatured = request.IsFeatured,
            FeaturedLabel = request.FeaturedLabel,
            FeaturedPosition = request.FeaturedPosition,
            Status = VideoStatus.Draft,
            SortOrder = request.SortOrder,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _videos.AddAsync(video);

        // Create child tag records
        if (request.Tags is { Count: > 0 })
        {
            for (var i = 0; i < request.Tags.Count; i++)
            {
                var tag = new LibraryVideoTag
                {
                    Id = Guid.NewGuid(),
                    VideoId = videoId,
                    Tag = request.Tags[i],
                    SortOrder = i
                };
                await _tags.AddAsync(tag);
            }
        }

        // Create child feature records
        if (request.Features is { Count: > 0 })
        {
            for (var i = 0; i < request.Features.Count; i++)
            {
                var feature = new LibraryVideoFeature
                {
                    Id = Guid.NewGuid(),
                    VideoId = videoId,
                    Icon = request.Features[i].Icon,
                    Description = request.Features[i].Description,
                    SortOrder = i
                };
                await _features.AddAsync(feature);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // Evict library tree cache for all known location codes
        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("Library video {VideoId} created with slug {Slug}. Cache evicted.", videoId, request.Slug);

        return videoId;
    }
}
