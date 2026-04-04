using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.AddLibraryVideoPrice;

/// <summary>Handles <see cref="AddLibraryVideoPriceCommand"/>.</summary>
public sealed class AddLibraryVideoPriceCommandHandler : IRequestHandler<AddLibraryVideoPriceCommand, Guid>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryVideo> _videos;
    private readonly IRepository<LibraryVideoPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AddLibraryVideoPriceCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public AddLibraryVideoPriceCommandHandler(IRepository<LibraryVideo> videos, IRepository<LibraryVideoPrice> prices, IUnitOfWork uow, IMemoryCache cache, ILogger<AddLibraryVideoPriceCommandHandler> logger)
    { _videos = videos; _prices = prices; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Creates a video price override.</summary>
    public async Task<Guid> Handle(AddLibraryVideoPriceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding price override for video {VideoId}, location {Location}", request.VideoId, request.LocationCode);
        if (!await _videos.AnyAsync(v => v.Id == request.VideoId && !v.IsDeleted, cancellationToken))
            throw new NotFoundException(nameof(LibraryVideo), request.VideoId);
        if (await _prices.AnyAsync(p => p.VideoId == request.VideoId && p.LocationCode == request.LocationCode, cancellationToken))
            throw new DomainException($"A price override already exists for video {request.VideoId} at location {request.LocationCode}.");
        var price = new LibraryVideoPrice
        {
            Id = Guid.NewGuid(), VideoId = request.VideoId, LocationCode = request.LocationCode.ToUpperInvariant(),
            Amount = request.Amount, CurrencyCode = request.CurrencyCode, CurrencySymbol = request.CurrencySymbol,
            OriginalAmount = request.OriginalAmount, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        await _prices.AddAsync(price);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
        _logger.LogInformation("Video price override {PriceId} created", price.Id);
        return price.Id;
    }
}
