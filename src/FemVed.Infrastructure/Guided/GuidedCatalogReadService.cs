using FemVed.Application.Guided.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FemVed.Infrastructure.Guided;

/// <summary>
/// Implements <see cref="IGuidedCatalogReadService"/> using EF Core projections over
/// <see cref="AppDbContext"/>. All queries are read-only (AsNoTracking + AsSplitQuery).
/// </summary>
public sealed class GuidedCatalogReadService : IGuidedCatalogReadService
{
    private readonly AppDbContext _context;
    private readonly ILogger<GuidedCatalogReadService> _logger;

    /// <summary>Initialises the service with the db context and logger.</summary>
    public GuidedCatalogReadService(AppDbContext context, ILogger<GuidedCatalogReadService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<GuidedTreeResponse> GetGuidedTreeAsync(
        string locationCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading guided tree for location {LocationCode}", locationCode);

        var domains = await _context.GuidedDomains
            .AsNoTracking()
            .AsSplitQuery()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .Include(d => d.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.WhatsIncluded.OrderBy(w => w.SortOrder))
            .Include(d => d.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.KeyAreas.OrderBy(k => k.SortOrder))
            .Include(d => d.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Programs
                    .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.SortOrder))
                    .ThenInclude(p => p.Expert)
            .Include(d => d.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Programs
                    .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.SortOrder))
                    .ThenInclude(p => p.WhatYouGet.OrderBy(w => w.SortOrder))
            .Include(d => d.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Programs
                    .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.SortOrder))
                    .ThenInclude(p => p.WhoIsThisFor.OrderBy(w => w.SortOrder))
            .Include(d => d.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Programs
                    .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.SortOrder))
                    .ThenInclude(p => p.Durations
                        .Where(dur => dur.IsActive)
                        .OrderBy(dur => dur.SortOrder))
                        .ThenInclude(dur => dur.Prices.Where(pr => pr.IsActive))
            .Include(d => d.Categories
                .Where(c => c.IsActive && c.ParentId == null)
                .OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Programs
                    .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.SortOrder))
                    .ThenInclude(p => p.DetailSections.OrderBy(s => s.SortOrder))
            .ToListAsync(cancellationToken);

        var response = new GuidedTreeResponse(
            domains.Select(d => MapDomain(d, locationCode)).ToList());

        _logger.LogInformation("Guided tree loaded: {DomainCount} domains for location {LocationCode}",
            domains.Count, locationCode);

        return response;
    }

    /// <inheritdoc/>
    public async Task<GuidedCategoryDto?> GetCategoryBySlugAsync(
        string slug,
        string locationCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading category slug {Slug} for location {LocationCode}", slug, locationCode);

        var category = await _context.GuidedCategories
            .AsNoTracking()
            .AsSplitQuery()
            .Where(c => c.Slug == slug && c.IsActive)
            .Include(c => c.WhatsIncluded.OrderBy(w => w.SortOrder))
            .Include(c => c.KeyAreas.OrderBy(k => k.SortOrder))
            .Include(c => c.Programs
                .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Expert)
            .Include(c => c.Programs
                .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.WhatYouGet.OrderBy(w => w.SortOrder))
            .Include(c => c.Programs
                .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.WhoIsThisFor.OrderBy(w => w.SortOrder))
            .Include(c => c.Programs
                .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.Durations
                    .Where(dur => dur.IsActive)
                    .OrderBy(dur => dur.SortOrder))
                    .ThenInclude(dur => dur.Prices.Where(pr => pr.IsActive))
            .Include(c => c.Programs
                .Where(p => p.Status == ProgramStatus.Published && !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.SortOrder))
                .ThenInclude(p => p.DetailSections.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
            return null;

        return MapCategory(category, locationCode);
    }

    /// <inheritdoc/>
    public async Task<ProgramInCategoryDto?> GetProgramBySlugAsync(
        string slug,
        string locationCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading program slug {Slug} for location {LocationCode}", slug, locationCode);

        var program = await _context.Programs
            .AsNoTracking()
            .AsSplitQuery()
            .Where(p => p.Slug == slug
                     && p.Status == ProgramStatus.Published
                     && !p.IsDeleted
                     && p.IsActive)
            .Include(p => p.Expert)
            .Include(p => p.WhatYouGet.OrderBy(w => w.SortOrder))
            .Include(p => p.WhoIsThisFor.OrderBy(w => w.SortOrder))
            .Include(p => p.Durations
                .Where(dur => dur.IsActive)
                .OrderBy(dur => dur.SortOrder))
                .ThenInclude(dur => dur.Prices.Where(pr => pr.IsActive))
            .Include(p => p.DetailSections.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(cancellationToken);

        if (program is null)
            return null;

        return MapProgram(program, locationCode);
    }

    // ── Private mapping helpers ───────────────────────────────────────────────

    private static GuidedDomainDto MapDomain(GuidedDomain domain, string locationCode) =>
        new(
            DomainId: domain.Id,
            DomainName: domain.Name,
            Categories: domain.Categories
                .Select(c => MapCategory(c, locationCode))
                .ToList());

    private static GuidedCategoryDto MapCategory(GuidedCategory category, string locationCode) =>
        new(
            CategoryId: category.Id,
            CategoryName: category.Slug,
            CategoryPageData: new CategoryPageDataDto(
                CategoryPageDataImage: category.ImageUrl,
                CategoryType: category.CategoryType,
                CategoryHeroTitle: category.HeroTitle,
                CategoryHeroSubtext: category.HeroSubtext,
                CategoryCtaLabel: category.CtaLabel,
                CategoryCtaTo: category.CtaLink,
                WhatsIncludedInCategory: category.WhatsIncluded
                    .Select(w => w.ItemText)
                    .ToList(),
                CategoryPageHeader: category.PageHeader,
                CategoryPageKeyAreas: category.KeyAreas
                    .Select(k => k.AreaText)
                    .ToList()),
            ProgramsInCategory: category.Programs
                .Select(p => MapProgram(p, locationCode))
                .ToList());

    private static ProgramInCategoryDto MapProgram(Program program, string locationCode) =>
        new(
            ProgramId: program.Id,
            ProgramName: program.Name,
            ProgramGridDescription: program.GridDescription,
            ProgramGridImage: program.GridImageUrl,
            ExpertId: program.Expert.Id,
            ExpertName: program.Expert.DisplayName,
            ExpertTitle: program.Expert.Title,
            ExpertGridDescription: program.Expert.GridDescription,
            ExpertDetailedDescription: program.Expert.DetailedDescription,
            ProgramDurations: program.Durations
                .Select(d => new ProgramDurationDto(
                    DurationId: d.Id,
                    DurationLabel: d.Label,
                    DurationPrice: FormatPrice(d.Prices, locationCode)))
                .ToList(),
            ProgramPageDisplayDetails: new ProgramPageDisplayDetailsDto(
                Overview: program.Overview,
                WhatYouGet: program.WhatYouGet
                    .Select(w => w.ItemText)
                    .ToList(),
                WhoIsThisFor: program.WhoIsThisFor
                    .Select(w => w.ItemText)
                    .ToList(),
                DetailSections: program.DetailSections
                    .Select(s => new ProgramDetailSectionDto(s.Heading, s.Description))
                    .ToList()));

    /// <summary>
    /// Selects the best-match price for the given location code, falling back to GB,
    /// then any active price. Returns "N/A" if no prices exist.
    /// </summary>
    private static string FormatPrice(IEnumerable<DurationPrice> prices, string locationCode)
    {
        var list = prices.ToList();

        var price = list.FirstOrDefault(p => p.LocationCode == locationCode)
                 ?? list.FirstOrDefault(p => p.LocationCode == "GB")
                 ?? list.FirstOrDefault();

        if (price is null)
            return "N/A";

        // e.g. "£320", "₹33,000", "$400"
        return $"{price.CurrencySymbol}{price.Amount:N0}";
    }
}
