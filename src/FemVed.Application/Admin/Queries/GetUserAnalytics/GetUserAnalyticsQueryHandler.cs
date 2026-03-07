using System.Globalization;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetUserAnalytics;

/// <summary>
/// Handles <see cref="GetUserAnalyticsQuery"/>.
/// Computes user registration stats, repeat/conversion ratios, and 12-month cohort analysis.
/// </summary>
public sealed class GetUserAnalyticsQueryHandler : IRequestHandler<GetUserAnalyticsQuery, UserAnalyticsDto>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Order> _orders;
    private readonly ILogger<GetUserAnalyticsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetUserAnalyticsQueryHandler(
        IRepository<User> users,
        IRepository<Order> orders,
        ILogger<GetUserAnalyticsQueryHandler> logger)
    {
        _users  = users;
        _orders = orders;
        _logger = logger;
    }

    /// <summary>Returns user analytics and cohort data.</summary>
    public async Task<UserAnalyticsDto> Handle(GetUserAnalyticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetUserAnalytics: computing user analytics");

        var users      = await _users.GetAllAsync(u => !u.IsDeleted, cancellationToken);
        var paidOrders = await _orders.GetAllAsync(o => o.Status == OrderStatus.Paid, cancellationToken);

        // ── Core stats ───────────────────────────────────────────────────────
        var totalRegistered = users.Count;

        // First purchase date per user (minimum paid order date)
        var firstPurchaseByUser = paidOrders
            .GroupBy(o => o.UserId)
            .ToDictionary(g => g.Key, g => g.Min(o => o.CreatedAt));

        var totalBuyers   = firstPurchaseByUser.Count;
        var repeatBuyers  = paidOrders
            .GroupBy(o => o.UserId)
            .Count(g => g.Count() > 1);

        var repeatRatio     = totalBuyers > 0 ? Math.Round((decimal)repeatBuyers / totalBuyers * 100, 2) : 0m;
        var conversionRate  = totalRegistered > 0 ? Math.Round((decimal)totalBuyers / totalRegistered * 100, 2) : 0m;

        // ── Monthly new users (last 12 months) ───────────────────────────────
        var now      = DateTimeOffset.UtcNow;
        var cutoff   = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-11);
        var months   = Enumerable.Range(0, 12).Select(i => cutoff.AddMonths(i)).ToList();

        var usersByMonth = users
            .GroupBy(u => (u.CreatedAt.Year, u.CreatedAt.Month))
            .ToDictionary(g => g.Key, g => g.ToList());

        var newUsersByMonth = months.Select(m =>
        {
            usersByMonth.TryGetValue((m.Year, m.Month), out var usersInMonth);
            usersInMonth ??= new List<User>();

            var buyersInMonth = usersInMonth.Count(u => firstPurchaseByUser.ContainsKey(u.Id));
            var label = m.ToString("MMM yyyy", CultureInfo.InvariantCulture);
            return new MonthlyUserStatsDto(m.Year, m.Month, label, usersInMonth.Count, buyersInMonth);
        }).ToList();

        // ── Cohort analysis (last 12 months) ─────────────────────────────────
        var cohorts = months.Select(monthStart =>
        {
            var monthEnd = monthStart.AddMonths(1);
            usersByMonth.TryGetValue((monthStart.Year, monthStart.Month), out var cohortUsers);
            cohortUsers ??= new List<User>();

            var label = monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture);

            if (cohortUsers.Count == 0)
                return new CohortDto(monthStart.Year, monthStart.Month, label, 0, 0, 0, 0, 0m);

            var bought30 = 0; var bought60 = 0; var bought90 = 0;

            foreach (var user in cohortUsers)
            {
                if (!firstPurchaseByUser.TryGetValue(user.Id, out var firstPurchase)) continue;
                var days = (firstPurchase - user.CreatedAt).TotalDays;
                if (days <= 30) bought30++;
                if (days <= 60) bought60++;
                if (days <= 90) bought90++;
            }

            var rate30 = Math.Round((decimal)bought30 / cohortUsers.Count * 100, 2);
            return new CohortDto(monthStart.Year, monthStart.Month, label,
                cohortUsers.Count, bought30, bought60, bought90, rate30);
        }).ToList();

        _logger.LogInformation("GetUserAnalytics: completed. Registered={Reg}, Buyers={Buyers}, RepeatRatio={Ratio}%",
            totalRegistered, totalBuyers, repeatRatio);

        return new UserAnalyticsDto(
            TotalRegistered:  totalRegistered,
            TotalBuyers:      totalBuyers,
            RepeatBuyers:     repeatBuyers,
            RepeatRatio:      repeatRatio,
            ConversionRate:   conversionRate,
            NewUsersByMonth:  newUsersByMonth,
            Cohorts:          cohorts);
    }
}
