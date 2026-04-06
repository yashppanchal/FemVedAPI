using System.Globalization;
using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetLibraryAnalytics;

/// <summary>
/// Handles <see cref="GetLibraryAnalyticsQuery"/>.
/// Loads all library-related data and computes analytics in memory.
/// </summary>
public sealed class GetLibraryAnalyticsQueryHandler
    : IRequestHandler<GetLibraryAnalyticsQuery, LibraryAnalyticsDto>
{
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["GBP"] = "£",  ["USD"] = "$",   ["INR"] = "₹",  ["AUD"] = "A$",
        ["EUR"] = "€",  ["AED"] = "د.إ", ["NZD"] = "NZ$", ["SGD"] = "S$",
        ["MYR"] = "RM", ["ZAR"] = "R",   ["LKR"] = "₨"
    };

    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<Order> _orders;
    private readonly IRepository<UserLibraryAccess> _access;
    private readonly ILogger<GetLibraryAnalyticsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetLibraryAnalyticsQueryHandler(
        IRepository<LibraryVideo> videos,
        IRepository<Order> orders,
        IRepository<UserLibraryAccess> access,
        ILogger<GetLibraryAnalyticsQueryHandler> logger)
    {
        _videos = videos;
        _orders = orders;
        _access = access;
        _logger = logger;
    }

    /// <summary>Returns aggregated library analytics.</summary>
    public async Task<LibraryAnalyticsDto> Handle(
        GetLibraryAnalyticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetLibraryAnalytics: computing library analytics");

        // ── Videos ──────────────────────────────────────────────────────────
        var allVideos = await _videos.GetAllAsync(cancellationToken: cancellationToken);
        var totalVideos     = allVideos.Count;
        var publishedVideos = allVideos.Count(v => v.Status == VideoStatus.Published);
        var draftVideos     = allVideos.Count(v => v.Status == VideoStatus.Draft);
        var archivedVideos  = allVideos.Count(v => v.Status == VideoStatus.Archived);

        // ── Orders (library only) ───────────────────────────────────────────
        var allOrders = await _orders.GetAllAsync(
            o => o.OrderSource == OrderSource.Library, cancellationToken);
        var paidOrders = allOrders.Where(o => o.Status == OrderStatus.Paid).ToList();

        var totalOrders = allOrders.Count;
        var paidCount   = paidOrders.Count;
        var failedCount = allOrders.Count(o => o.Status == OrderStatus.Failed);

        // ── Purchases ───────────────────────────────────────────────────────
        var allAccess = await _access.GetAllAsync(cancellationToken: cancellationToken);
        var totalPurchases = allAccess.Count;

        // ── Revenue by currency ─────────────────────────────────────────────
        var revenueByCurrency = paidOrders
            .GroupBy(o => o.CurrencyCode)
            .Select(g => new LibraryCurrencyRevenueDto(
                g.Key,
                CurrencySymbols.GetValueOrDefault(g.Key, g.Key),
                g.Sum(o => o.AmountPaid),
                g.Count()))
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        // ── Top selling videos ──────────────────────────────────────────────
        var accessByVideo = allAccess
            .GroupBy(a => a.VideoId)
            .Select(g => new { VideoId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var videoLookup = allVideos.ToDictionary(v => v.Id);
        // Need expert names — load via query
        var expertIds = allVideos.Select(v => v.ExpertId).Distinct().ToHashSet();

        var topSelling = new List<LibraryTopVideoDto>();
        foreach (var item in accessByVideo)
        {
            if (videoLookup.TryGetValue(item.VideoId, out var video))
            {
                topSelling.Add(new LibraryTopVideoDto(
                    video.Id,
                    video.Title,
                    video.VideoType.ToString().ToUpperInvariant(),
                    item.Count,
                    video.Expert?.DisplayName ?? "Unknown"));
            }
        }

        // ── Revenue by month (last 12 months) ──────────────────────────────
        var cutoff = DateTimeOffset.UtcNow.AddMonths(-12);
        var revenueByMonth = paidOrders
            .Where(o => o.CreatedAt >= cutoff)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month, o.CurrencyCode })
            .Select(g => new LibraryMonthlyRevenueDto(
                g.Key.Year,
                g.Key.Month,
                new DateTimeOffset(g.Key.Year, g.Key.Month, 1, 0, 0, 0, TimeSpan.Zero)
                    .ToString("MMM yyyy", CultureInfo.InvariantCulture),
                g.Key.CurrencyCode,
                CurrencySymbols.GetValueOrDefault(g.Key.CurrencyCode, g.Key.CurrencyCode),
                g.Sum(o => o.AmountPaid),
                g.Count()))
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        _logger.LogInformation(
            "GetLibraryAnalytics: {Videos} videos, {Purchases} purchases, {PaidOrders} paid orders",
            totalVideos, totalPurchases, paidCount);

        return new LibraryAnalyticsDto(
            totalVideos, publishedVideos, draftVideos, archivedVideos,
            totalPurchases, totalOrders, paidCount, failedCount,
            revenueByCurrency, topSelling, revenueByMonth);
    }
}
