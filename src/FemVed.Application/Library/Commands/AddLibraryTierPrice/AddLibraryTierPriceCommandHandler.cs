using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.AddLibraryTierPrice;

/// <summary>Handles <see cref="AddLibraryTierPriceCommand"/>.</summary>
public sealed class AddLibraryTierPriceCommandHandler : IRequestHandler<AddLibraryTierPriceCommand, Guid>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryPriceTier> _tiers;
    private readonly IRepository<LibraryTierPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AddLibraryTierPriceCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public AddLibraryTierPriceCommandHandler(IRepository<LibraryPriceTier> tiers, IRepository<LibraryTierPrice> prices, IUnitOfWork uow, IMemoryCache cache, ILogger<AddLibraryTierPriceCommandHandler> logger)
    { _tiers = tiers; _prices = prices; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Creates a tier price.</summary>
    public async Task<Guid> Handle(AddLibraryTierPriceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding tier price for tier {TierId}, location {Location}", request.TierId, request.LocationCode);
        if (!await _tiers.AnyAsync(t => t.Id == request.TierId, cancellationToken))
            throw new NotFoundException(nameof(LibraryPriceTier), request.TierId);
        if (await _prices.AnyAsync(p => p.TierId == request.TierId && p.LocationCode == request.LocationCode, cancellationToken))
            throw new DomainException($"A price already exists for tier {request.TierId} at location {request.LocationCode}.");
        var price = new LibraryTierPrice
        {
            Id = Guid.NewGuid(), TierId = request.TierId,
            LocationCode = request.LocationCode.ToUpperInvariant(),
            Amount = request.Amount, CurrencyCode = request.CurrencyCode,
            CurrencySymbol = request.CurrencySymbol,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        await _prices.AddAsync(price);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
        _logger.LogInformation("Tier price {Id} created", price.Id);
        return price.Id;
    }
}
