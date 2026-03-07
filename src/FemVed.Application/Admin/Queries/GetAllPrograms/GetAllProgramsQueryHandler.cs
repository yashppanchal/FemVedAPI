using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAllPrograms;

/// <summary>
/// Handles <see cref="GetAllProgramsQuery"/>.
/// Loads all programs (including soft-deleted), batch-fetches experts and categories,
/// and maps to <see cref="AdminProgramDto"/>.
/// </summary>
public sealed class GetAllProgramsQueryHandler : IRequestHandler<GetAllProgramsQuery, List<AdminProgramDto>>
{
    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<GuidedCategory> _categories;
    private readonly ILogger<GetAllProgramsQueryHandler> _logger;

    /// <summary>Initialises the handler with required repositories.</summary>
    public GetAllProgramsQueryHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<GuidedCategory> categories,
        ILogger<GetAllProgramsQueryHandler> logger)
    {
        _programs   = programs;
        _experts    = experts;
        _categories = categories;
        _logger     = logger;
    }

    /// <summary>Returns all programs ordered by creation date descending, with optional status filter.</summary>
    /// <param name="request">The query, optionally containing a status filter string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Flat list of admin program DTOs.</returns>
    public async Task<List<AdminProgramDto>> Handle(GetAllProgramsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAllPrograms: loading all programs (statusFilter={StatusFilter})", request.StatusFilter);

        // Parse optional status filter
        ProgramStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
            Enum.TryParse<ProgramStatus>(request.StatusFilter, ignoreCase: true, out var parsed))
        {
            statusFilter = parsed;
        }

        // Load all programs (including soft-deleted) — admin sees everything
        var programs = statusFilter.HasValue
            ? await _programs.GetAllAsync(p => p.Status == statusFilter.Value, cancellationToken)
            : await _programs.GetAllAsync(cancellationToken: cancellationToken);

        if (programs.Count == 0)
        {
            _logger.LogInformation("GetAllPrograms: no programs found");
            return new List<AdminProgramDto>();
        }

        // Batch-load experts and categories
        var expertIds   = programs.Select(p => p.ExpertId).Distinct().ToHashSet();
        var categoryIds = programs.Select(p => p.CategoryId).Distinct().ToHashSet();

        var experts    = await _experts.GetAllAsync(e => expertIds.Contains(e.Id), cancellationToken);
        var categories = await _categories.GetAllAsync(c => categoryIds.Contains(c.Id), cancellationToken);

        var expertMap   = experts.ToDictionary(e => e.Id);
        var categoryMap = categories.ToDictionary(c => c.Id);

        var result = programs
            .OrderByDescending(p => p.CreatedAt)
            .Select(p =>
            {
                expertMap.TryGetValue(p.ExpertId, out var expert);
                categoryMap.TryGetValue(p.CategoryId, out var category);

                return new AdminProgramDto(
                    ProgramId:    p.Id,
                    Name:         p.Name,
                    Slug:         p.Slug,
                    Status:       p.Status.ToString(),
                    IsActive:     p.IsActive,
                    IsDeleted:    p.IsDeleted,
                    ExpertId:     p.ExpertId,
                    ExpertName:   expert?.DisplayName ?? "Unknown Expert",
                    CategoryId:   p.CategoryId,
                    CategoryName: category?.Name ?? "Unknown Category",
                    SortOrder:    p.SortOrder,
                    CreatedAt:    p.CreatedAt,
                    UpdatedAt:    p.UpdatedAt);
            })
            .ToList();

        _logger.LogInformation("GetAllPrograms: returned {Count} programs", result.Count);
        return result;
    }
}
