using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.DeleteLibraryVideoPrice;

/// <summary>Handles <see cref="DeleteLibraryVideoPriceCommand"/>.</summary>
public sealed class DeleteLibraryVideoPriceCommandHandler : IRequestHandler<DeleteLibraryVideoPriceCommand>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryVideoPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteLibraryVideoPriceCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeleteLibraryVideoPriceCommandHandler(IRepository<LibraryVideoPrice> prices, IUnitOfWork uow, IMemoryCache cache, ILogger<DeleteLibraryVideoPriceCommandHandler> logger)
    { _prices = prices; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Removes the price override.</summary>
    public async Task Handle(DeleteLibraryVideoPriceCommand request, CancellationToken cancellationToken)
    {
        var price = await _prices.FirstOrDefaultAsync(p => p.Id == request.PriceId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryVideoPrice), request.PriceId);
        _prices.Remove(price);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
    }
}
