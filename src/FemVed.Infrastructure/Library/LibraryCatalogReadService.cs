using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FemVed.Infrastructure.Library;

/// <summary>
/// Implements <see cref="ILibraryCatalogReadService"/> using EF Core projections over
/// <see cref="AppDbContext"/>. All queries are read-only (AsNoTracking + AsSplitQuery).
/// </summary>
public sealed class LibraryCatalogReadService : ILibraryCatalogReadService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LibraryCatalogReadService> _logger;

    /// <summary>Initialises the service with the db context and logger.</summary>
    public LibraryCatalogReadService(AppDbContext context, ILogger<LibraryCatalogReadService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<LibraryTreeResponse> GetLibraryTreeAsync(
        string locationCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading library tree for location {LocationCode}", locationCode);

        // Load the first active library domain with all related data
        var domain = await _context.LibraryDomains
            .AsNoTracking()
            .AsSplitQuery()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .Include(d => d.FilterTypes
                .Where(f => f.IsActive)
                .OrderBy(f => f.SortOrder))
            .Include(d => d.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Videos
                    .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                    .OrderBy(v => v.SortOrder))
                    .ThenInclude(v => v.Expert)
            .Include(d => d.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Videos
                    .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                    .OrderBy(v => v.SortOrder))
                    .ThenInclude(v => v.Tags.OrderBy(t => t.SortOrder))
            .Include(d => d.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Videos
                    .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                    .OrderBy(v => v.SortOrder))
                    .ThenInclude(v => v.Episodes)
            .Include(d => d.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Videos
                    .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                    .OrderBy(v => v.SortOrder))
                    .ThenInclude(v => v.PriceOverrides)
            .Include(d => d.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Videos
                    .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                    .OrderBy(v => v.SortOrder))
                    .ThenInclude(v => v.PriceTier)
                    .ThenInclude(t => t.Prices)
            .FirstOrDefaultAsync(cancellationToken);

        if (domain is null)
        {
            _logger.LogWarning("No active library domain found — returning empty tree");
            return new LibraryTreeResponse(
                new LibraryDomainDto(Guid.Empty, "Wellness Library", null, null, null),
                new List<LibraryFilterDto>(),
                new List<LibraryFeaturedVideoDto>(),
                new List<LibraryCategoryDto>());
        }

        // Build all published videos flat list for featured extraction
        var allVideos = domain.Categories
            .SelectMany(c => c.Videos)
            .ToList();

        var domainDto = new LibraryDomainDto(
            domain.Id, domain.Name,
            domain.HeroImageDesktop, domain.HeroImageMobile, domain.HeroImagePortrait);

        var filters = BuildFilters(domain.FilterTypes);

        var featured = allVideos
            .Where(v => v.IsFeatured && v.FeaturedPosition.HasValue)
            .OrderBy(v => v.FeaturedPosition)
            .Take(3)
            .Select(v => MapFeaturedVideo(v, locationCode))
            .ToList();

        var categories = domain.Categories
            .Select(c => MapCategory(c, locationCode))
            .ToList();

        _logger.LogInformation(
            "Library tree loaded: {CategoryCount} categories, {VideoCount} videos, {FeaturedCount} featured for location {LocationCode}",
            categories.Count, allVideos.Count, featured.Count, locationCode);

        return new LibraryTreeResponse(domainDto, filters, featured, categories);
    }

    /// <inheritdoc/>
    public async Task<LibraryCategoryDto?> GetCategoryBySlugAsync(
        string slug,
        string locationCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading library category slug {Slug} for location {LocationCode}", slug, locationCode);

        var category = await _context.LibraryCategories
            .AsNoTracking()
            .AsSplitQuery()
            .Where(c => c.Slug == slug && c.IsActive)
            .Include(c => c.Videos
                .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                .OrderBy(v => v.SortOrder))
                .ThenInclude(v => v.Expert)
            .Include(c => c.Videos
                .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                .OrderBy(v => v.SortOrder))
                .ThenInclude(v => v.Tags.OrderBy(t => t.SortOrder))
            .Include(c => c.Videos
                .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                .OrderBy(v => v.SortOrder))
                .ThenInclude(v => v.Episodes)
            .Include(c => c.Videos
                .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                .OrderBy(v => v.SortOrder))
                .ThenInclude(v => v.PriceOverrides)
            .Include(c => c.Videos
                .Where(v => v.Status == VideoStatus.Published && !v.IsDeleted)
                .OrderBy(v => v.SortOrder))
                .ThenInclude(v => v.PriceTier)
                .ThenInclude(t => t.Prices)
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
            return null;

        return MapCategory(category, locationCode);
    }

    /// <inheritdoc/>
    public async Task<LibraryVideoDetailResponse?> GetVideoBySlugAsync(
        string slug,
        string locationCode,
        Guid? currentUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading library video slug {Slug} for location {LocationCode}", slug, locationCode);

        var video = await _context.LibraryVideos
            .AsNoTracking()
            .AsSplitQuery()
            .Where(v => v.Slug == slug
                     && v.Status == VideoStatus.Published
                     && !v.IsDeleted)
            .Include(v => v.Expert)
            .Include(v => v.PriceTier)
                .ThenInclude(t => t.Prices)
            .Include(v => v.PriceOverrides)
            .Include(v => v.Episodes.OrderBy(e => e.SortOrder))
            .Include(v => v.Tags.OrderBy(t => t.SortOrder))
            .Include(v => v.Features.OrderBy(f => f.SortOrder))
            .Include(v => v.Testimonials
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder))
            .FirstOrDefaultAsync(cancellationToken);

        if (video is null)
            return null;

        // Check purchase status
        var isPurchased = false;
        if (currentUserId.HasValue)
        {
            isPurchased = await _context.UserLibraryAccess
                .AsNoTracking()
                .AnyAsync(a => a.UserId == currentUserId.Value
                            && a.VideoId == video.Id
                            && a.IsActive,
                    cancellationToken);
        }

        var (price, originalPrice) = ResolvePrice(video.PriceOverrides, video.PriceTier, locationCode);

        return new LibraryVideoDetailResponse(
            VideoId: video.Id,
            Title: video.Title,
            Slug: video.Slug,
            Synopsis: video.Synopsis,
            Description: video.Description,
            CardImage: video.CardImage,
            HeroImage: video.HeroImage,
            IconEmoji: video.IconEmoji,
            GradientClass: video.GradientClass,
            TrailerUrl: video.TrailerUrl,
            VideoType: video.VideoType.ToString().ToUpperInvariant(),
            TotalDuration: video.TotalDuration,
            ReleaseYear: video.ReleaseYear,
            Price: price,
            OriginalPrice: originalPrice,
            PriceTier: video.PriceTier.TierKey,
            ExpertId: video.Expert.Id,
            ExpertName: video.Expert.DisplayName,
            ExpertTitle: video.Expert.Title,
            ExpertGridDescription: video.Expert.GridDescription,
            Tags: video.Tags.Select(t => t.Tag).ToList(),
            Episodes: video.Episodes
                .Select(e => new LibraryEpisodeDto(
                    EpisodeId: e.Id,
                    EpisodeNumber: e.EpisodeNumber,
                    Title: e.Title,
                    Description: e.Description,
                    Duration: e.Duration,
                    IsFreePreview: e.IsFreePreview,
                    IsLocked: !isPurchased && !e.IsFreePreview))
                .ToList(),
            Features: video.Features
                .Select(f => new LibraryFeatureDto(f.Icon, f.Description))
                .ToList(),
            Testimonials: video.Testimonials
                .Select(t => new LibraryTestimonialDto(t.ReviewerName, t.ReviewText, t.Rating))
                .ToList(),
            IsPurchased: isPurchased);
    }

    /// <inheritdoc/>
    public async Task<List<LibraryFilterDto>> GetFilterTypesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading library filter types");

        var filters = await _context.LibraryFilterTypes
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(cancellationToken);

        return BuildFilters(filters);
    }

    /// <inheritdoc/>
    public async Task<MyLibraryResponse> GetMyLibraryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading library for user {UserId}", userId);

        var accessRecords = await _context.UserLibraryAccess
            .AsNoTracking()
            .AsSplitQuery()
            .Where(a => a.UserId == userId && a.IsActive)
            .Include(a => a.Video).ThenInclude(v => v.Expert)
            .Include(a => a.Video).ThenInclude(v => v.Episodes)
            .Include(a => a.Video).ThenInclude(v => v.Category)
            .OrderByDescending(a => a.PurchasedAt)
            .ToListAsync(cancellationToken);

        var videos = accessRecords
            .Select(a => new MyLibraryVideoDto(
                VideoId: a.Video.Id,
                Title: a.Video.Title,
                Slug: a.Video.Slug,
                CardImage: a.Video.CardImage,
                IconEmoji: a.Video.IconEmoji,
                GradientClass: a.Video.GradientClass,
                VideoType: a.Video.VideoType.ToString().ToUpperInvariant(),
                TotalDuration: a.Video.TotalDuration,
                EpisodeCount: a.Video.VideoType == VideoType.Series
                    ? a.Video.Episodes.Count
                    : null,
                ExpertName: a.Video.Expert.DisplayName,
                PurchasedAt: a.PurchasedAt,
                WatchProgressSeconds: a.WatchProgressSeconds,
                LastWatchedAt: a.LastWatchedAt,
                CategorySlug: a.Video.Category.Slug))
            .ToList();

        _logger.LogInformation("Returned {Count} purchased videos for user {UserId}",
            videos.Count, userId);

        return new MyLibraryResponse(videos);
    }

    // ── Private mapping helpers ───────────────────────────────────────────────

    /// <summary>
    /// Builds the filter list with a synthetic "All Programs" entry prepended.
    /// </summary>
    private static List<LibraryFilterDto> BuildFilters(IEnumerable<LibraryFilterType> dbFilters) =>
        new List<LibraryFilterDto>
        {
            new(Guid.Empty, "All Programs", "all", "ALL")
        }
        .Concat(dbFilters.Select(f => new LibraryFilterDto(
            f.Id,
            f.Name,
            f.FilterKey,
            f.FilterTarget.ToString().ToUpperInvariant())))
        .ToList();

    private static LibraryCategoryDto MapCategory(LibraryCategory category, string locationCode) =>
        new(
            CategoryId: category.Id,
            CategoryName: category.Name,
            CategorySlug: category.Slug,
            Videos: category.Videos
                .Select(v => MapVideoCard(v, locationCode))
                .ToList());

    private static LibraryVideoCardDto MapVideoCard(LibraryVideo video, string locationCode)
    {
        var (price, originalPrice) = ResolvePrice(video.PriceOverrides, video.PriceTier, locationCode);
        var episodeCount = video.VideoType == VideoType.Series
            ? video.Episodes.Count
            : (int?)null;

        return new LibraryVideoCardDto(
            VideoId: video.Id,
            Title: video.Title,
            Slug: video.Slug,
            Synopsis: video.Synopsis,
            CardImage: video.CardImage,
            IconEmoji: video.IconEmoji,
            GradientClass: video.GradientClass,
            VideoType: video.VideoType.ToString().ToUpperInvariant(),
            TotalDuration: video.TotalDuration,
            EpisodeCount: episodeCount,
            ReleaseYear: video.ReleaseYear,
            Price: price,
            OriginalPrice: originalPrice,
            ExpertName: video.Expert.DisplayName,
            ExpertTitle: video.Expert.Title,
            Tags: video.Tags.Select(t => t.Tag).ToList());
    }

    private static LibraryFeaturedVideoDto MapFeaturedVideo(LibraryVideo video, string locationCode)
    {
        var (price, _) = ResolvePrice(video.PriceOverrides, video.PriceTier, locationCode);
        var episodeCount = video.VideoType == VideoType.Series
            ? video.Episodes.Count
            : (int?)null;

        return new LibraryFeaturedVideoDto(
            VideoId: video.Id,
            Position: video.FeaturedPosition,
            EyebrowText: video.FeaturedLabel,
            Title: video.Title,
            ExpertName: video.Expert.DisplayName,
            TotalDuration: video.TotalDuration,
            EpisodeCount: episodeCount,
            Price: price,
            CardImage: video.CardImage,
            IconEmoji: video.IconEmoji,
            GradientClass: video.GradientClass,
            VideoType: video.VideoType.ToString().ToUpperInvariant());
    }

    /// <summary>
    /// Resolves the display price for a video at a given location.
    /// Priority: per-video override → tier default. Location fallback: exact → GB → any.
    /// Returns (formattedPrice, formattedOriginalPrice).
    /// </summary>
    private static (string Price, string? OriginalPrice) ResolvePrice(
        ICollection<LibraryVideoPrice> overrides,
        LibraryPriceTier tier,
        string locationCode)
    {
        // 1. Check per-video price overrides
        var videoPrice = overrides.FirstOrDefault(p => p.LocationCode == locationCode)
                      ?? overrides.FirstOrDefault(p => p.LocationCode == "GB")
                      ?? overrides.FirstOrDefault();

        if (videoPrice is not null)
        {
            var formatted = FormatAmount(videoPrice.CurrencySymbol, videoPrice.Amount);
            var formattedOriginal = videoPrice.OriginalAmount.HasValue
                ? FormatAmount(videoPrice.CurrencySymbol, videoPrice.OriginalAmount.Value)
                : null;
            return (formatted, formattedOriginal);
        }

        // 2. Fall back to tier prices
        var tierPrices = tier.Prices.ToList();
        var tierPrice = tierPrices.FirstOrDefault(p => p.LocationCode == locationCode)
                     ?? tierPrices.FirstOrDefault(p => p.LocationCode == "GB")
                     ?? tierPrices.FirstOrDefault();

        if (tierPrice is not null)
            return (FormatAmount(tierPrice.CurrencySymbol, tierPrice.Amount), null);

        return ("N/A", null);
    }

    /// <summary>Formats a currency amount for display, e.g. "£320", "₹33,000", "$400".</summary>
    private static string FormatAmount(string symbol, decimal amount) =>
        $"{symbol}{amount:N0}";
}
