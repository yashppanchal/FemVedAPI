using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateLibraryVideoPrice;

/// <summary>Handles <see cref="UpdateLibraryVideoPriceCommand"/>.</summary>
public sealed class UpdateLibraryVideoPriceCommandHandler : IRequestHandler<UpdateLibraryVideoPriceCommand>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryVideoPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateLibraryVideoPriceCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public UpdateLibraryVideoPriceCommandHandler(IRepository<LibraryVideoPrice> prices, IUnitOfWork uow, IMemoryCache cache, ILogger<UpdateLibraryVideoPriceCommandHandler> logger)
    { _prices = prices; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Updates the price override.</summary>
    public async Task Handle(UpdateLibraryVideoPriceCommand request, CancellationToken cancellationToken)
    {
        var price = await _prices.FirstOrDefaultAsync(p => p.Id == request.PriceId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideoPrice), request.PriceId);
        if (request.Amount.HasValue) price.Amount = request.Amount.Value;
        if (request.CurrencyCode is not null) price.CurrencyCode = request.CurrencyCode;
        if (request.CurrencySymbol is not null) price.CurrencySymbol = request.CurrencySymbol;
        if (request.OriginalAmount.HasValue) price.OriginalAmount = request.OriginalAmount;
        price.UpdatedAt = DateTimeOffset.UtcNow;
        _prices.Update(price);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
    }
}
