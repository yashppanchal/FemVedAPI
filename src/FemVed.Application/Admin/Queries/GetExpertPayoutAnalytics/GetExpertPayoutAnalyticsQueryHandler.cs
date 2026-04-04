using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetExpertPayoutAnalytics;

/// <summary>
/// Handles <see cref="GetExpertPayoutAnalyticsQuery"/>.
/// Computes each expert's total earned, share (after commission), amount already paid,
/// and outstanding balance — all broken down by currency.
/// </summary>
public sealed class GetExpertPayoutAnalyticsQueryHandler
    : IRequestHandler<GetExpertPayoutAnalyticsQuery, List<ExpertPayoutBalanceDto>>
{
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["GBP"] = "£",  ["USD"] = "$",   ["INR"] = "₹",  ["AUD"] = "A$",
        ["EUR"] = "€",  ["AED"] = "د.إ", ["NZD"] = "NZ$",["SGD"] = "S$",
        ["MYR"] = "RM", ["ZAR"] = "R",   ["LKR"] = "₨"
    };

    private readonly IRepository<Expert> _experts;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<Order> _orders;
    private readonly IRepository<ExpertPayout> _payouts;
    private readonly ILogger<GetExpertPayoutAnalyticsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetExpertPayoutAnalyticsQueryHandler(
        IRepository<Expert> experts,
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<Order> orders,
        IRepository<ExpertPayout> payouts,
        ILogger<GetExpertPayoutAnalyticsQueryHandler> logger)
    {
        _experts  = experts;
        _programs = programs;
        _durations= durations;
        _orders   = orders;
        _payouts  = payouts;
        _logger   = logger;
    }

    /// <summary>Returns a payout balance sheet for every expert.</summary>
    public async Task<List<ExpertPayoutBalanceDto>> Handle(
        GetExpertPayoutAnalyticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetExpertPayoutAnalytics: computing payout balance sheets");

        var experts    = await _experts.GetAllAsync(e => !e.IsDeleted, cancellationToken);
        var programs   = await _programs.GetAllAsync(p => !p.IsDeleted, cancellationToken);
        var durations  = await _durations.GetAllAsync(cancellationToken: cancellationToken);
        var paidOrders = await _orders.GetAllAsync(o => o.Status == OrderStatus.Paid, cancellationToken);
        var allPayouts = await _payouts.GetAllAsync(cancellationToken: cancellationToken);

        // durationId → programId → expertId
        var programByDuration = durations.ToDictionary(d => d.Id, d => d.ProgramId);
        var expertByProgram   = programs.ToDictionary(p => p.Id, p => p.ExpertId);

        // expertId → list of paid orders (via duration → program → expert)
        var ordersByExpert = paidOrders
            .Where(o => programByDuration.ContainsKey(o.DurationId.GetValueOrDefault())
                     && expertByProgram.ContainsKey(programByDuration[o.DurationId.GetValueOrDefault()]))
            .GroupBy(o => expertByProgram[programByDuration[o.DurationId.GetValueOrDefault()]])
            .ToDictionary(g => g.Key, g => g.ToList());

        // expertId → list of payout records
        var payoutsByExpert = allPayouts
            .GroupBy(p => p.ExpertId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = experts.Select(e =>
        {
            ordersByExpert.TryGetValue(e.Id, out var expertOrders);
            expertOrders ??= new List<Order>();

            payoutsByExpert.TryGetValue(e.Id, out var expertPayouts);
            expertPayouts ??= new List<ExpertPayout>();

            // Total earned per currency
            var totalEarned = expertOrders
                .GroupBy(o => o.CurrencyCode)
                .Select(g => new CurrencyAmountDto(
                    g.Key,
                    CurrencySymbols.GetValueOrDefault(g.Key, g.Key),
                    g.Sum(o => o.AmountPaid),
                    g.Count()))
                .ToList();

            // Expert share and platform commission
            var expertShare = totalEarned.Select(r => new CurrencyAmountDto(
                r.CurrencyCode, r.CurrencySymbol,
                Math.Round(r.Amount * (e.CommissionRate / 100m), 2))).ToList();

            var platformCommission = totalEarned.Select(r => new CurrencyAmountDto(
                r.CurrencyCode, r.CurrencySymbol,
                Math.Round(r.Amount * ((100m - e.CommissionRate) / 100m), 2))).ToList();

            // Already paid per currency
            var paidByCurrency = expertPayouts
                .GroupBy(p => p.CurrencyCode)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            var totalPaid = paidByCurrency.Select(kvp => new CurrencyAmountDto(
                kvp.Key,
                CurrencySymbols.GetValueOrDefault(kvp.Key, kvp.Key),
                kvp.Value)).ToList();

            // Outstanding = expert share minus paid (per currency)
            var allCurrencies = expertShare.Select(s => s.CurrencyCode)
                .Union(totalPaid.Select(p => p.CurrencyCode))
                .Distinct();

            var outstanding = allCurrencies.Select(code =>
            {
                var share = expertShare.FirstOrDefault(s => s.CurrencyCode == code)?.Amount ?? 0m;
                var paid  = paidByCurrency.GetValueOrDefault(code, 0m);
                return new CurrencyAmountDto(
                    code,
                    CurrencySymbols.GetValueOrDefault(code, code),
                    Math.Round(share - paid, 2));
            }).ToList();

            var lastPayout = expertPayouts.Count > 0
                ? expertPayouts.Max(p => p.PaidAt)
                : (DateTimeOffset?)null;

            return new ExpertPayoutBalanceDto(
                ExpertId:          e.Id,
                ExpertName:        e.DisplayName,
                CommissionRate:    e.CommissionRate,
                TotalEarned:       totalEarned,
                ExpertShare:       expertShare,
                PlatformCommission:platformCommission,
                TotalPaid:         totalPaid,
                OutstandingBalance:outstanding,
                LastPayoutAt:      lastPayout);
        })
        .OrderBy(e => e.ExpertName)
        .ToList();

        _logger.LogInformation("GetExpertPayoutAnalytics: completed for {Count} experts", result.Count);
        return result;
    }
}
