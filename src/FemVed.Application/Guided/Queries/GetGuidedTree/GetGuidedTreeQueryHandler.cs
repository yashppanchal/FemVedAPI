using FemVed.Application.Guided.DTOs;
using FemVed.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Queries.GetGuidedTree;

/// <summary>
/// Handles <see cref="GetGuidedTreeQuery"/>.
/// Delegates to <see cref="IGuidedCatalogReadService"/> for the EF Core projection,
/// and wraps the result in a 10-minute memory cache keyed by location code.
/// </summary>
public sealed class GetGuidedTreeQueryHandler : IRequestHandler<GetGuidedTreeQuery, GuidedTreeResponse>
{
    /// <summary>Cache key prefix for the guided tree. Append location code to get the full key.</summary>
    public const string CacheKeyPrefix = "guided_tree_";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IGuidedCatalogReadService _readService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetGuidedTreeQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetGuidedTreeQueryHandler(
        IGuidedCatalogReadService readService,
        IMemoryCache cache,
        ILogger<GetGuidedTreeQueryHandler> logger)
    {
        _readService = readService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Returns the cached guided tree, refreshing it if the cache has expired.</summary>
    /// <param name="request">The query containing the location code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full guided tree response.</returns>
    public async Task<GuidedTreeResponse> Handle(GetGuidedTreeQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{request.LocationCode}";

        if (_cache.TryGetValue(cacheKey, out GuidedTreeResponse? cached) && cached is not null)
        {
            _logger.LogInformation("Guided tree served from cache for location {LocationCode}", request.LocationCode);
            return cached;
        }

        _logger.LogInformation("Building guided tree for location {LocationCode}", request.LocationCode);
        var result = await _readService.GetGuidedTreeAsync(request.LocationCode, cancellationToken);

        _cache.Set(cacheKey, result, CacheDuration);
        _logger.LogInformation("Guided tree cached for location {LocationCode}", request.LocationCode);

        return result;
    }
}
