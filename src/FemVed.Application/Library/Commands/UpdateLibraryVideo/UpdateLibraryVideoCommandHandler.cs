using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateLibraryVideo;

/// <summary>
/// Handles <see cref="UpdateLibraryVideoCommand"/>.
/// Applies non-null fields, replaces tags/features when provided, and evicts the library tree cache.
/// </summary>
public sealed class UpdateLibraryVideoCommandHandler : IRequestHandler<UpdateLibraryVideoCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryVideoTag> _tags;
    private readonly IRepository<LibraryVideoFeature> _features;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateLibraryVideoCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateLibraryVideoCommandHandler(
        IRepository<LibraryVideo> videos,
        IRepository<LibraryVideoTag> tags,
        IRepository<LibraryVideoFeature> features,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<UpdateLibraryVideoCommandHandler> logger)
    {
        _videos = videos;
        _tags = tags;
        _features = features;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Updates the library video, replacing tags and features when provided.
    /// </summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the video is not found or is soft-deleted.</exception>
    /// <exception cref="DomainException">Thrown when the new slug is already in use.</exception>
    public async Task Handle(UpdateLibraryVideoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating library video {VideoId}", request.VideoId);

        var video = await _videos.FirstOrDefaultAsync(
            v => v.Id == request.VideoId && !v.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("LibraryVideo", request.VideoId);

        // Apply non-null scalar fields
        if (request.CategoryId.HasValue) video.CategoryId = request.CategoryId.Value;
        if (request.PriceTierId.HasValue) video.PriceTierId = request.PriceTierId.Value;
        if (request.Title is not null) video.Title = request.Title;
        if (request.Synopsis is not null) video.Synopsis = request.Synopsis;
        if (request.Description is not null) video.Description = request.Description;
        if (request.CardImage is not null) video.CardImage = request.CardImage;
        if (request.HeroImage is not null) video.HeroImage = request.HeroImage;
        if (request.IconEmoji is not null) video.IconEmoji = request.IconEmoji;
        if (request.GradientClass is not null) video.GradientClass = request.GradientClass;
        if (request.TrailerUrl is not null) video.TrailerUrl = request.TrailerUrl;
        if (request.StreamUrl is not null) video.StreamUrl = request.StreamUrl;
        if (request.TotalDuration is not null) video.TotalDuration = request.TotalDuration;
        if (request.TotalDurationSeconds.HasValue) video.TotalDurationSeconds = request.TotalDurationSeconds.Value;
        if (request.ReleaseYear is not null) video.ReleaseYear = request.ReleaseYear;
        if (request.IsFeatured.HasValue) video.IsFeatured = request.IsFeatured.Value;
        if (request.FeaturedLabel is not null) video.FeaturedLabel = request.FeaturedLabel;
        if (request.FeaturedPosition.HasValue) video.FeaturedPosition = request.FeaturedPosition.Value;
        if (request.SortOrder.HasValue) video.SortOrder = request.SortOrder.Value;

        // If slug changed, check uniqueness
        if (request.Slug is not null && request.Slug != video.Slug)
        {
            var slugTaken = await _videos.AnyAsync(
                v => v.Slug == request.Slug && v.Id != request.VideoId && !v.IsDeleted, cancellationToken);
            if (slugTaken)
                throw new DomainException($"A library video with slug '{request.Slug}' already exists.");

            video.Slug = request.Slug;
        }

        // Replace tags if provided
        if (request.Tags is not null)
        {
            var existingTags = await _tags.GetAllAsync(
                t => t.VideoId == request.VideoId, cancellationToken);
            foreach (var tag in existingTags)
                _tags.Remove(tag);

            for (var i = 0; i < request.Tags.Count; i++)
            {
                var tag = new LibraryVideoTag
                {
                    Id = Guid.NewGuid(),
                    VideoId = request.VideoId,
                    Tag = request.Tags[i],
                    SortOrder = i
                };
                await _tags.AddAsync(tag);
            }
        }

        // Replace features if provided
        if (request.Features is not null)
        {
            var existingFeatures = await _features.GetAllAsync(
                f => f.VideoId == request.VideoId, cancellationToken);
            foreach (var feature in existingFeatures)
                _features.Remove(feature);

            for (var i = 0; i < request.Features.Count; i++)
            {
                var feature = new LibraryVideoFeature
                {
                    Id = Guid.NewGuid(),
                    VideoId = request.VideoId,
                    Icon = request.Features[i].Icon,
                    Description = request.Features[i].Description,
                    SortOrder = i
                };
                await _features.AddAsync(feature);
            }
        }

        video.UpdatedAt = DateTimeOffset.UtcNow;
        _videos.Update(video);
        await _uow.SaveChangesAsync(cancellationToken);

        // Evict library tree cache for all known location codes
        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("Library video {VideoId} updated. Cache evicted.", request.VideoId);
    }
}
