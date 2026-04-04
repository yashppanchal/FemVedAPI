using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetLibraryTree;

/// <summary>
/// Handles <see cref="GetLibraryTreeQuery"/>.
/// Delegates to <see cref="ILibraryCatalogReadService"/> for the EF Core projection,
/// and wraps the result in a 10-minute memory cache keyed by location code.
/// </summary>
public sealed class GetLibraryTreeQueryHandler : IRequestHandler<GetLibraryTreeQuery, LibraryTreeResponse>
{
    /// <summary>Cache key prefix for the library tree. Append location code to get the full key.</summary>
    public const string CacheKeyPrefix = "library_tree_";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly ILibraryCatalogReadService _readService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetLibraryTreeQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetLibraryTreeQueryHandler(
        ILibraryCatalogReadService readService,
        IMemoryCache cache,
        ILogger<GetLibraryTreeQueryHandler> logger)
    {
        _readService = readService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Returns the cached library tree, refreshing it if the cache has expired.</summary>
    /// <param name="request">The query containing the location code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full library tree response.</returns>
    public async Task<LibraryTreeResponse> Handle(GetLibraryTreeQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{request.LocationCode}";

        if (_cache.TryGetValue(cacheKey, out LibraryTreeResponse? cached) && cached is not null)
        {
            _logger.LogInformation("Library tree served from cache for location {LocationCode}", request.LocationCode);
            return cached;
        }

        _logger.LogInformation("Building library tree for location {LocationCode}", request.LocationCode);
        var result = await _readService.GetLibraryTreeAsync(request.LocationCode, cancellationToken);

        _cache.Set(cacheKey, result, CacheDuration);
        _logger.LogInformation("Library tree cached for location {LocationCode}", request.LocationCode);

        return result;
    }
}
