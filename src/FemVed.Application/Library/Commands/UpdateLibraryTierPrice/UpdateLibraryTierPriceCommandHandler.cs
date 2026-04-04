using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateLibraryTierPrice;

/// <summary>Handles <see cref="UpdateLibraryTierPriceCommand"/>.</summary>
public sealed class UpdateLibraryTierPriceCommandHandler : IRequestHandler<UpdateLibraryTierPriceCommand>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryTierPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateLibraryTierPriceCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public UpdateLibraryTierPriceCommandHandler(IRepository<LibraryTierPrice> prices, IUnitOfWork uow, IMemoryCache cache, ILogger<UpdateLibraryTierPriceCommandHandler> logger)
    { _prices = prices; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Updates the tier price.</summary>
    public async Task Handle(UpdateLibraryTierPriceCommand request, CancellationToken cancellationToken)
    {
        var price = await _prices.FirstOrDefaultAsync(p => p.Id == request.PriceId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryTierPrice), request.PriceId);
        if (request.Amount.HasValue) price.Amount = request.Amount.Value;
        if (request.CurrencyCode is not null) price.CurrencyCode = request.CurrencyCode;
        if (request.CurrencySymbol is not null) price.CurrencySymbol = request.CurrencySymbol;
        price.UpdatedAt = DateTimeOffset.UtcNow;
        _prices.Update(price);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
    }
}
