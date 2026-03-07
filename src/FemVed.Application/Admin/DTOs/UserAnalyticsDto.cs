namespace FemVed.Application.Admin.DTOs;

// ── User Analytics & Cohorts ──────────────────────────────────────────────────

/// <summary>User registration, purchase behaviour, repeat ratio, and cohort analytics.</summary>
/// <param name="TotalRegistered">Total non-deleted users.</param>
/// <param name="TotalBuyers">Users who have at least one paid order.</param>
/// <param name="RepeatBuyers">Users who have more than one paid order.</param>
/// <param name="RepeatRatio">RepeatBuyers / TotalBuyers × 100 (percentage).</param>
/// <param name="ConversionRate">TotalBuyers / TotalRegistered × 100 (percentage).</param>
/// <param name="NewUsersByMonth">New registration and buyer counts for the last 12 months.</param>
/// <param name="Cohorts">Monthly cohort table showing purchase conversion rates.</param>
public record UserAnalyticsDto(
    int TotalRegistered,
    int TotalBuyers,
    int RepeatBuyers,
    decimal RepeatRatio,
    decimal ConversionRate,
    List<MonthlyUserStatsDto> NewUsersByMonth,
    List<CohortDto> Cohorts);

/// <summary>Monthly registration and buyer counts.</summary>
/// <param name="Year">Calendar year.</param>
/// <param name="Month">Calendar month (1–12).</param>
/// <param name="MonthLabel">Human-readable label, e.g. "Mar 2026".</param>
/// <param name="NewUsers">Users who registered in this month.</param>
/// <param name="NewBuyers">Of the new users, how many made at least one purchase.</param>
public record MonthlyUserStatsDto(
    int Year,
    int Month,
    string MonthLabel,
    int NewUsers,
    int NewBuyers);

/// <summary>
/// Cohort analysis for users who registered in a given month.
/// Shows the percentage who made their first purchase within 30, 60, and 90 days.
/// </summary>
/// <param name="Year">Cohort registration year.</param>
/// <param name="Month">Cohort registration month (1–12).</param>
/// <param name="MonthLabel">Human-readable label, e.g. "Jan 2026".</param>
/// <param name="UsersRegistered">Total users who registered in this month.</param>
/// <param name="PurchasedWithin30Days">How many bought within 30 days of registration.</param>
/// <param name="PurchasedWithin60Days">How many bought within 60 days of registration.</param>
/// <param name="PurchasedWithin90Days">How many bought within 90 days of registration.</param>
/// <param name="Rate30Days">PurchasedWithin30Days / UsersRegistered × 100.</param>
public record CohortDto(
    int Year,
    int Month,
    string MonthLabel,
    int UsersRegistered,
    int PurchasedWithin30Days,
    int PurchasedWithin60Days,
    int PurchasedWithin90Days,
    decimal Rate30Days);
