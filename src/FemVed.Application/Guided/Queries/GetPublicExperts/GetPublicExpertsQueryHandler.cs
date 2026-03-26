using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Queries.GetPublicExperts;

/// <summary>
/// Handles <see cref="GetPublicExpertsQuery"/>.
/// Returns active experts with their published program count, cached for 10 minutes.
/// </summary>
public sealed class GetPublicExpertsQueryHandler : IRequestHandler<GetPublicExpertsQuery, List<PublicExpertDto>>
{
    internal const string CacheKey = "public_experts_list";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IRepository<Expert> _experts;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetPublicExpertsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetPublicExpertsQueryHandler(
        IRepository<Expert> experts,
        IRepository<Domain.Entities.Program> programs,
        IMemoryCache cache,
        ILogger<GetPublicExpertsQueryHandler> logger)
    {
        _experts  = experts;
        _programs = programs;
        _cache    = cache;
        _logger   = logger;
    }

    /// <summary>Returns the cached or freshly-loaded public experts list.</summary>
    public async Task<List<PublicExpertDto>> Handle(GetPublicExpertsQuery request, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out List<PublicExpertDto>? cached) && cached is not null)
            return cached;

        _logger.LogInformation("GetPublicExperts: loading from database");

        var activeExperts = await _experts.GetAllAsync(
            e => e.IsActive && !e.IsDeleted, cancellationToken);

        var publishedPrograms = await _programs.GetAllAsync(
            p => p.Status == ProgramStatus.Published && !p.IsDeleted, cancellationToken);

        var programCountByExpert = publishedPrograms
            .GroupBy(p => p.ExpertId)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = activeExperts
            .Select(e => new PublicExpertDto(
                ExpertId:              e.Id,
                DisplayName:           e.DisplayName,
                Title:                 e.Title,
                GridDescription:       e.GridDescription,
                ProfileImageUrl:       e.ProfileImageUrl,
                GridImageUrl:          e.GridImageUrl,
                Specialisations:       e.Specialisations,
                YearsExperience:       e.YearsExperience,
                LocationCountry:       e.LocationCountry,
                PublishedProgramCount: programCountByExpert.GetValueOrDefault(e.Id, 0)))
            .OrderBy(e => e.DisplayName)
            .ToList();

        _cache.Set(CacheKey, result, CacheDuration);
        _logger.LogInformation("GetPublicExperts: returned {Count} experts, cached for {Minutes}m", result.Count, CacheDuration.TotalMinutes);

        return result;
    }
}
