using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Queries.GetMyEarnings;

/// <summary>
/// Handles <see cref="GetMyEarningsQuery"/>.
/// Computes the authenticated expert's full financial summary:
/// total revenue from their programs, their payout share (commissionRate %),
/// platform profit, total already paid, and outstanding balance — all per currency.
/// </summary>
public sealed class GetMyEarningsQueryHandler
    : IRequestHandler<GetMyEarningsQuery, ExpertPayoutBalanceDto>
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
    private readonly ILogger<GetMyEarningsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetMyEarningsQueryHandler(
        IRepository<Expert> experts,
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<Order> orders,
        IRepository<ExpertPayout> payouts,
        ILogger<GetMyEarningsQueryHandler> logger)
    {
        _experts   = experts;
        _programs  = programs;
        _durations = durations;
        _orders    = orders;
        _payouts   = payouts;
        _logger    = logger;
    }

    /// <summary>
    /// Returns the earnings and payout balance sheet for the authenticated expert.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when no expert profile is linked to the user.</exception>
    public async Task<ExpertPayoutBalanceDto> Handle(
        GetMyEarningsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetMyEarnings: computing earnings for user {UserId}", request.UserId);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.UserId == request.UserId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Expert profile", request.UserId);

        // Load only this expert's programs
        var programs = await _programs.GetAllAsync(
            p => p.ExpertId == expert.Id && !p.IsDeleted, cancellationToken);

        var programIds = programs.Select(p => p.Id).ToHashSet();

        // Load durations for those programs only
        var durations = await _durations.GetAllAsync(
            d => programIds.Contains(d.ProgramId), cancellationToken);

        var durationIds = durations.Select(d => d.Id).ToHashSet();

        // Load paid orders that belong to those durations
        var paidOrders = await _orders.GetAllAsync(
            o => o.Status == OrderStatus.Paid && durationIds.Contains(o.DurationId),
            cancellationToken);

        // Load all payout records for this expert
        var expertPayouts = await _payouts.GetAllAsync(
            p => p.ExpertId == expert.Id, cancellationToken);

        // ── Total earned per currency ─────────────────────────────────────────
        var totalEarned = paidOrders
            .GroupBy(o => o.CurrencyCode)
            .Select(g => new CurrencyAmountDto(
                g.Key,
                CurrencySymbols.GetValueOrDefault(g.Key, g.Key),
                g.Sum(o => o.AmountPaid),
                g.Count()))
            .ToList();

        // ── Expert share (commissionRate % of earned) ─────────────────────────
        var expertShare = totalEarned.Select(r => new CurrencyAmountDto(
            r.CurrencyCode, r.CurrencySymbol,
            Math.Round(r.Amount * (expert.CommissionRate / 100m), 2))).ToList();

        // ── Platform profit ((100 - commissionRate) % of earned) ─────────────
        var platformProfit = totalEarned.Select(r => new CurrencyAmountDto(
            r.CurrencyCode, r.CurrencySymbol,
            Math.Round(r.Amount * ((100m - expert.CommissionRate) / 100m), 2))).ToList();

        // ── Total already paid to this expert per currency ────────────────────
        var paidByCurrency = expertPayouts
            .GroupBy(p => p.CurrencyCode)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var totalPaid = paidByCurrency.Select(kvp => new CurrencyAmountDto(
            kvp.Key,
            CurrencySymbols.GetValueOrDefault(kvp.Key, kvp.Key),
            kvp.Value)).ToList();

        // ── Outstanding balance = expert share minus paid ─────────────────────
        var allCurrencies = expertShare.Select(s => s.CurrencyCode)
            .Union(totalPaid.Select(p => p.CurrencyCode))
            .Distinct();

        var outstandingBalance = allCurrencies.Select(code =>
        {
            var share = expertShare.FirstOrDefault(s => s.CurrencyCode == code)?.Amount ?? 0m;
            var paid  = paidByCurrency.GetValueOrDefault(code, 0m);
            return new CurrencyAmountDto(
                code,
                CurrencySymbols.GetValueOrDefault(code, code),
                Math.Round(share - paid, 2));
        }).ToList();

        var lastPayoutAt = expertPayouts.Count > 0
            ? expertPayouts.Max(p => p.PaidAt)
            : (DateTimeOffset?)null;

        _logger.LogInformation(
            "GetMyEarnings: completed for expert {ExpertId} — {OrderCount} paid orders, {PayoutCount} payouts received",
            expert.Id, paidOrders.Count, expertPayouts.Count);

        return new ExpertPayoutBalanceDto(
            ExpertId:          expert.Id,
            ExpertName:        expert.DisplayName,
            CommissionRate:    expert.CommissionRate,
            TotalEarned:       totalEarned,
            ExpertShare:       expertShare,
            PlatformCommission:platformProfit,
            TotalPaid:         totalPaid,
            OutstandingBalance:outstandingBalance,
            LastPayoutAt:      lastPayoutAt);
    }
}
