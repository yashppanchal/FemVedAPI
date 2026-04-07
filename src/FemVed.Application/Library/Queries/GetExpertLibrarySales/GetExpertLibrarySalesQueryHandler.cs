using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetExpertLibrarySales;

/// <summary>Handles <see cref="GetExpertLibrarySalesQuery"/>.</summary>
public sealed class GetExpertLibrarySalesQueryHandler
    : IRequestHandler<GetExpertLibrarySalesQuery, ExpertLibrarySalesResponse>
{
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["GBP"] = "£", ["USD"] = "$", ["INR"] = "₹", ["AUD"] = "A$",
        ["EUR"] = "€", ["AED"] = "د.إ"
    };

    private readonly IRepository<Expert> _experts;
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<UserLibraryAccess> _access;
    private readonly IRepository<Order> _orders;
    private readonly ILogger<GetExpertLibrarySalesQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetExpertLibrarySalesQueryHandler(
        IRepository<Expert> experts,
        IRepository<LibraryVideo> videos,
        IRepository<UserLibraryAccess> access,
        IRepository<Order> orders,
        ILogger<GetExpertLibrarySalesQueryHandler> logger)
    {
        _experts = experts;
        _videos = videos;
        _access = access;
        _orders = orders;
        _logger = logger;
    }

    /// <summary>Returns library video sales data for the expert.</summary>
    public async Task<ExpertLibrarySalesResponse> Handle(
        GetExpertLibrarySalesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading library sales for user {UserId}", request.UserId);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.UserId == request.UserId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Expert profile", request.UserId);

        // Load expert's library videos
        var videos = await _videos.GetAllAsync(
            v => v.ExpertId == expert.Id && !v.IsDeleted, cancellationToken);

        if (videos.Count == 0)
        {
            return new ExpertLibrarySalesResponse(0, 0,
                new List<ExpertLibraryVideoSalesDto>(),
                new List<ExpertLibrarySalesCurrencyDto>());
        }

        var videoIds = videos.Select(v => v.Id).ToHashSet();

        // Load all access records for these videos
        var accessRecords = await _access.GetAllAsync(
            a => videoIds.Contains(a.VideoId) && a.IsActive, cancellationToken);

        // Load paid orders for these access records
        var orderIds = accessRecords.Select(a => a.OrderId).ToHashSet();
        var paidOrders = await _orders.GetAllAsync(
            o => orderIds.Contains(o.Id) && o.Status == OrderStatus.Paid, cancellationToken);

        var orderMap = paidOrders.ToDictionary(o => o.Id);

        // Build per-video sales count
        var purchasesByVideo = accessRecords.GroupBy(a => a.VideoId)
            .ToDictionary(g => g.Key, g => g.Count());

        var videoSales = videos
            .OrderByDescending(v => purchasesByVideo.GetValueOrDefault(v.Id, 0))
            .Select(v => new ExpertLibraryVideoSalesDto(
                v.Id,
                v.Title,
                v.VideoType.ToString().ToUpperInvariant(),
                v.Status.ToString().ToUpperInvariant(),
                purchasesByVideo.GetValueOrDefault(v.Id, 0),
                v.CreatedAt))
            .ToList();

        // Build revenue by currency from paid orders linked to these videos
        var relevantOrders = accessRecords
            .Where(a => orderMap.ContainsKey(a.OrderId))
            .Select(a => orderMap[a.OrderId])
            .ToList();

        var revenueByCurrency = relevantOrders
            .GroupBy(o => o.CurrencyCode)
            .Select(g => new ExpertLibrarySalesCurrencyDto(
                g.Key,
                CurrencySymbols.GetValueOrDefault(g.Key, g.Key),
                Math.Round(g.Sum(o => o.AmountPaid), 2),
                g.Count()))
            .OrderByDescending(r => r.TotalRevenue)
            .ToList();

        var totalPurchases = accessRecords.Count;

        _logger.LogInformation(
            "Expert {ExpertId} has {VideoCount} videos, {PurchaseCount} purchases",
            expert.Id, videos.Count, totalPurchases);

        return new ExpertLibrarySalesResponse(
            videos.Count, totalPurchases, videoSales, revenueByCurrency);
    }
}
