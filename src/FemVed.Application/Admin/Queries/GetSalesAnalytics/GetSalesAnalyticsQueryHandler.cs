using System.Globalization;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetSalesAnalytics;

/// <summary>
/// Handles <see cref="GetSalesAnalyticsQuery"/>.
/// Loads all orders and computes sales analytics entirely in memory.
/// </summary>
public sealed class GetSalesAnalyticsQueryHandler : IRequestHandler<GetSalesAnalyticsQuery, SalesAnalyticsDto>
{
    private static readonly Dictionary<string, string> CurrencySymbols = new()
    {
        ["GBP"] = "£",  ["USD"] = "$",   ["INR"] = "₹",  ["AUD"] = "A$",
        ["EUR"] = "€",  ["AED"] = "د.إ", ["NZD"] = "NZ$",["SGD"] = "S$",
        ["MYR"] = "RM", ["ZAR"] = "R",   ["LKR"] = "₨"
    };

    private readonly IRepository<Order> _orders;
    private readonly ILogger<GetSalesAnalyticsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetSalesAnalyticsQueryHandler(
        IRepository<Order> orders,
        ILogger<GetSalesAnalyticsQueryHandler> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    /// <summary>Returns aggregated sales analytics.</summary>
    public async Task<SalesAnalyticsDto> Handle(GetSalesAnalyticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetSalesAnalytics: computing sales analytics");

        var allOrders  = await _orders.GetAllAsync(cancellationToken: cancellationToken);
        var paidOrders = allOrders.Where(o => o.Status == OrderStatus.Paid).ToList();

        // ── Order funnel counts ──────────────────────────────────────────────
        var totalOrders    = allOrders.Count;
        var paidCount      = paidOrders.Count;
        var pendingCount   = allOrders.Count(o => o.Status == OrderStatus.Pending);
        var failedCount    = allOrders.Count(o => o.Status == OrderStatus.Failed);
        var refundedCount  = allOrders.Count(o => o.Status == OrderStatus.Refunded);
        var discountOrders = paidOrders.Count(o => o.DiscountAmount > 0);
        var totalDiscount  = paidOrders.Sum(o => o.DiscountAmount);

        // ── Revenue by currency ──────────────────────────────────────────────
        var byCurrency = paidOrders
            .GroupBy(o => o.CurrencyCode)
            .Select(g =>
            {
                var sym   = CurrencySymbols.GetValueOrDefault(g.Key, g.Key);
                var total = g.Sum(o => o.AmountPaid);
                var cnt   = g.Count();
                return new CurrencySalesDto(g.Key, sym, total, cnt,
                    cnt > 0 ? Math.Round(total / cnt, 2) : 0m);
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        // ── Revenue by gateway + currency ────────────────────────────────────
        var byGateway = paidOrders
            .GroupBy(o => (Gateway: o.PaymentGateway.ToString(), o.CurrencyCode))
            .Select(g => new GatewaySalesDto(
                g.Key.Gateway,
                g.Key.CurrencyCode,
                CurrencySymbols.GetValueOrDefault(g.Key.CurrencyCode, g.Key.CurrencyCode),
                g.Sum(o => o.AmountPaid),
                g.Count()))
            .OrderBy(x => x.Gateway).ThenBy(x => x.CurrencyCode)
            .ToList();

        // ── Revenue by country + currency ────────────────────────────────────
        var byCountry = paidOrders
            .GroupBy(o => (o.LocationCode, o.CurrencyCode))
            .Select(g => new CountrySalesDto(
                g.Key.LocationCode,
                g.Key.CurrencyCode,
                CurrencySymbols.GetValueOrDefault(g.Key.CurrencyCode, g.Key.CurrencyCode),
                g.Sum(o => o.AmountPaid),
                g.Count()))
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();

        // ── Monthly revenue — last 12 months, per currency ───────────────────
        var now       = DateTimeOffset.UtcNow;
        var cutoff    = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-11);
        var recentPaid = paidOrders.Where(o => o.CreatedAt >= cutoff).ToList();

        var byMonth = recentPaid
            .GroupBy(o => (o.CreatedAt.Year, o.CreatedAt.Month, o.CurrencyCode))
            .Select(g =>
            {
                var label = new DateTimeOffset(g.Key.Year, g.Key.Month, 1, 0, 0, 0, TimeSpan.Zero)
                    .ToString("MMM yyyy", CultureInfo.InvariantCulture);
                return new MonthlySalesDto(
                    g.Key.Year, g.Key.Month, label,
                    g.Key.CurrencyCode,
                    CurrencySymbols.GetValueOrDefault(g.Key.CurrencyCode, g.Key.CurrencyCode),
                    g.Sum(o => o.AmountPaid),
                    g.Count());
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.CurrencyCode)
            .ToList();

        _logger.LogInformation("GetSalesAnalytics: completed. PaidOrders={Paid}, Currencies={Currencies}",
            paidCount, byCurrency.Count);

        return new SalesAnalyticsDto(
            TotalOrders:       totalOrders,
            PaidOrders:        paidCount,
            PendingOrders:     pendingCount,
            FailedOrders:      failedCount,
            RefundedOrders:    refundedCount,
            OrdersWithDiscount:discountOrders,
            TotalDiscountGiven:totalDiscount,
            RevenueByCurrentcy:byCurrency,
            RevenueByGateway:  byGateway,
            RevenueByCountry:  byCountry,
            RevenueByMonth:    byMonth);
    }
}
