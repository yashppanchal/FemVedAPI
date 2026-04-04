using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetAllLibraryPriceTiers;

/// <summary>Handles <see cref="GetAllLibraryPriceTiersQuery"/>.</summary>
public sealed class GetAllLibraryPriceTiersQueryHandler
    : IRequestHandler<GetAllLibraryPriceTiersQuery, List<AdminPriceTierDto>>
{
    private readonly IRepository<LibraryPriceTier> _tiers;
    private readonly IRepository<LibraryTierPrice> _prices;
    private readonly ILogger<GetAllLibraryPriceTiersQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllLibraryPriceTiersQueryHandler(IRepository<LibraryPriceTier> tiers, IRepository<LibraryTierPrice> prices, ILogger<GetAllLibraryPriceTiersQueryHandler> logger)
    { _tiers = tiers; _prices = prices; _logger = logger; }

    /// <summary>Returns all tiers with prices.</summary>
    public async Task<List<AdminPriceTierDto>> Handle(GetAllLibraryPriceTiersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading all library price tiers for admin");
        var tiers = await _tiers.GetAllAsync(null, cancellationToken);
        var prices = await _prices.GetAllAsync(null, cancellationToken);
        var priceMap = prices.GroupBy(p => p.TierId).ToDictionary(g => g.Key, g => g.ToList());
        return tiers.OrderBy(t => t.SortOrder)
            .Select(t => new AdminPriceTierDto(
                t.Id, t.TierKey, t.DisplayName, t.SortOrder, t.IsActive,
                (priceMap.GetValueOrDefault(t.Id) ?? [])
                    .Select(p => new AdminTierPriceDto(p.Id, p.LocationCode, p.Amount, p.CurrencyCode, p.CurrencySymbol))
                    .ToList()))
            .ToList();
    }
}
