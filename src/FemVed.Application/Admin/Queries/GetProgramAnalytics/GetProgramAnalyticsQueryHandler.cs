using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetProgramAnalytics;

/// <summary>
/// Handles <see cref="GetProgramAnalyticsQuery"/>.
/// Batch-loads programs, experts, categories, durations, paid orders, and access records,
/// then computes per-program and per-expert analytics entirely in memory.
/// </summary>
public sealed class GetProgramAnalyticsQueryHandler : IRequestHandler<GetProgramAnalyticsQuery, ProgramAnalyticsDto>
{
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["GBP"] = "£",  ["USD"] = "$",   ["INR"] = "₹",  ["AUD"] = "A$",
        ["EUR"] = "€",  ["AED"] = "د.إ", ["NZD"] = "NZ$",["SGD"] = "S$",
        ["MYR"] = "RM", ["ZAR"] = "R",   ["LKR"] = "₨"
    };

    private static readonly HashSet<UserProgramAccessStatus> ActiveStatuses = new()
    {
        UserProgramAccessStatus.NotStarted,
        UserProgramAccessStatus.Active,
        UserProgramAccessStatus.Paused
    };

    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<GuidedCategory> _categories;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Order> _orders;
    private readonly IRepository<UserProgramAccess> _access;
    private readonly ILogger<GetProgramAnalyticsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetProgramAnalyticsQueryHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<Expert> experts,
        IRepository<GuidedCategory> categories,
        IRepository<ProgramDuration> durations,
        IRepository<Order> orders,
        IRepository<UserProgramAccess> access,
        ILogger<GetProgramAnalyticsQueryHandler> logger)
    {
        _programs   = programs;
        _experts    = experts;
        _categories = categories;
        _durations  = durations;
        _orders     = orders;
        _access     = access;
        _logger     = logger;
    }

    /// <summary>Returns per-program and per-expert performance analytics.</summary>
    public async Task<ProgramAnalyticsDto> Handle(GetProgramAnalyticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetProgramAnalytics: computing program analytics");

        // ── Batch load all data ──────────────────────────────────────────────
        var programs   = await _programs.GetAllAsync(p => !p.IsDeleted, cancellationToken);
        var experts    = await _experts.GetAllAsync(e => !e.IsDeleted, cancellationToken);
        var categories = await _categories.GetAllAsync(cancellationToken: cancellationToken);
        var durations  = await _durations.GetAllAsync(cancellationToken: cancellationToken);
        var paidOrders = await _orders.GetAllAsync(o => o.Status == OrderStatus.Paid, cancellationToken);
        var allAccess  = await _access.GetAllAsync(cancellationToken: cancellationToken);

        // ── Build lookup maps ────────────────────────────────────────────────
        var expertMap   = experts.ToDictionary(e => e.Id);
        var categoryMap = categories.ToDictionary(c => c.Id);

        // durationId → programId
        var programByDuration = durations.ToDictionary(d => d.Id, d => d.ProgramId);

        // programId → list of paid orders
        var ordersByProgram = paidOrders
            .Where(o => programByDuration.ContainsKey(o.DurationId))
            .GroupBy(o => programByDuration[o.DurationId])
            .ToDictionary(g => g.Key, g => g.ToList());

        // programId → access records
        var accessByProgram = allAccess
            .GroupBy(a => a.ProgramId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // ── Per-program stats ────────────────────────────────────────────────
        var programStats = programs.Select(p =>
        {
            expertMap.TryGetValue(p.ExpertId, out var expert);
            categoryMap.TryGetValue(p.CategoryId, out var cat);

            ordersByProgram.TryGetValue(p.Id, out var orders);
            orders ??= new List<Order>();

            accessByProgram.TryGetValue(p.Id, out var accesses);
            accesses ??= new List<UserProgramAccess>();

            var revenue = orders
                .GroupBy(o => o.CurrencyCode)
                .Select(g => new CurrencyAmountDto(
                    g.Key,
                    CurrencySymbols.GetValueOrDefault(g.Key, g.Key),
                    g.Sum(o => o.AmountPaid),
                    g.Count()))
                .ToList();

            return new ProgramStatsDto(
                ProgramId:            p.Id,
                ProgramName:          p.Name,
                ExpertName:           expert?.DisplayName ?? "Unknown",
                CategoryName:         cat?.Name ?? "Unknown",
                Status:               p.Status.ToString(),
                TotalSales:           orders.Count,
                ActiveEnrollments:    accesses.Count(a => ActiveStatuses.Contains(a.Status)),
                CompletedEnrollments: accesses.Count(a => a.Status == UserProgramAccessStatus.Completed),
                TotalEnrollments:     accesses.Count,
                Revenue:              revenue);
        })
        .OrderByDescending(p => p.TotalSales)
        .ToList();

        // ── Per-expert revenue ───────────────────────────────────────────────
        var expertStats = experts.Select(e =>
        {
            var expertPrograms = programs.Where(p => p.ExpertId == e.Id).ToList();
            var expertProgramIds = expertPrograms.Select(p => p.Id).ToHashSet();

            // Aggregate orders across all expert programs
            var expertOrders = ordersByProgram
                .Where(kvp => expertProgramIds.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .ToList();

            // Aggregate access records
            var expertAccesses = accessByProgram
                .Where(kvp => expertProgramIds.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .ToList();

            var totalRevenue = expertOrders
                .GroupBy(o => o.CurrencyCode)
                .Select(g => new CurrencyAmountDto(
                    g.Key,
                    CurrencySymbols.GetValueOrDefault(g.Key, g.Key),
                    g.Sum(o => o.AmountPaid),
                    g.Count()))
                .ToList();

            var expertShare = totalRevenue.Select(r => r with
            {
                Amount = Math.Round(r.Amount * (e.CommissionRate / 100m), 2),
                OrderCount = 0
            }).ToList();

            var platformRevenue = totalRevenue.Select(r => r with
            {
                Amount = Math.Round(r.Amount * ((100m - e.CommissionRate) / 100m), 2),
                OrderCount = 0
            }).ToList();

            return new ExpertRevenueDto(
                ExpertId:         e.Id,
                ExpertName:       e.DisplayName,
                CommissionRate:   e.CommissionRate,
                TotalSales:       expertOrders.Count,
                TotalEnrollments: expertAccesses.Count,
                ActiveEnrollments:expertAccesses.Count(a => ActiveStatuses.Contains(a.Status)),
                TotalRevenue:     totalRevenue,
                ExpertShare:      expertShare,
                PlatformRevenue:  platformRevenue);
        })
        .OrderByDescending(e => e.TotalSales)
        .ToList();

        _logger.LogInformation("GetProgramAnalytics: completed. Programs={Programs}, Experts={Experts}",
            programStats.Count, expertStats.Count);

        return new ProgramAnalyticsDto(Programs: programStats, Experts: expertStats);
    }
}
