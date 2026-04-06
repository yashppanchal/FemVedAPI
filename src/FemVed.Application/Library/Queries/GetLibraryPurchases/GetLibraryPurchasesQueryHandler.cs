using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetLibraryPurchases;

/// <summary>
/// Handles <see cref="GetLibraryPurchasesQuery"/>.
/// Returns all library purchase records with user/video/order details.
/// </summary>
public sealed class GetLibraryPurchasesQueryHandler
    : IRequestHandler<GetLibraryPurchasesQuery, LibraryPurchasesResponse>
{
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["GBP"] = "£", ["USD"] = "$", ["INR"] = "₹", ["AUD"] = "A$",
        ["EUR"] = "€", ["AED"] = "د.إ", ["NZD"] = "NZ$", ["SGD"] = "S$",
        ["MYR"] = "RM", ["ZAR"] = "R", ["LKR"] = "₨"
    };

    private readonly IRepository<UserLibraryAccess> _access;
    private readonly IRepository<Order> _orders;
    private readonly IRepository<User> _users;
    private readonly IRepository<LibraryVideo> _videos;
    private readonly ILogger<GetLibraryPurchasesQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetLibraryPurchasesQueryHandler(
        IRepository<UserLibraryAccess> access,
        IRepository<Order> orders,
        IRepository<User> users,
        IRepository<LibraryVideo> videos,
        ILogger<GetLibraryPurchasesQueryHandler> logger)
    {
        _access = access;
        _orders = orders;
        _users = users;
        _videos = videos;
        _logger = logger;
    }

    /// <summary>Returns all library purchases.</summary>
    public async Task<LibraryPurchasesResponse> Handle(
        GetLibraryPurchasesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetLibraryPurchases: loading purchase records");

        var allAccess = await _access.GetAllAsync(cancellationToken: cancellationToken);

        // Batch-load related entities
        var userIds  = allAccess.Select(a => a.UserId).Distinct().ToHashSet();
        var videoIds = allAccess.Select(a => a.VideoId).Distinct().ToHashSet();
        var orderIds = allAccess.Select(a => a.OrderId).Distinct().ToHashSet();

        var users  = (await _users.GetAllAsync(u => userIds.Contains(u.Id), cancellationToken))
            .ToDictionary(u => u.Id);
        var videos = (await _videos.GetAllAsync(v => videoIds.Contains(v.Id), cancellationToken))
            .ToDictionary(v => v.Id);
        var orders = (await _orders.GetAllAsync(o => orderIds.Contains(o.Id), cancellationToken))
            .ToDictionary(o => o.Id);

        var purchases = allAccess
            .OrderByDescending(a => a.PurchasedAt)
            .Select(a =>
            {
                var user  = users.GetValueOrDefault(a.UserId);
                var video = videos.GetValueOrDefault(a.VideoId);
                var order = orders.GetValueOrDefault(a.OrderId);

                return new LibraryPurchaseDto(
                    a.Id,
                    a.UserId,
                    user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown",
                    user?.Email ?? "",
                    a.VideoId,
                    video?.Title ?? "Unknown",
                    video?.VideoType.ToString().ToUpperInvariant() ?? "",
                    a.OrderId,
                    order?.AmountPaid ?? 0m,
                    order?.CurrencyCode ?? "",
                    CurrencySymbols.GetValueOrDefault(order?.CurrencyCode ?? "", ""),
                    a.PurchasedAt,
                    a.IsActive);
            })
            .ToList();

        _logger.LogInformation("GetLibraryPurchases: returning {Count} purchases", purchases.Count);

        return new LibraryPurchasesResponse(purchases);
    }
}
