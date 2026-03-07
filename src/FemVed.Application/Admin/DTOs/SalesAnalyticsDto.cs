namespace FemVed.Application.Admin.DTOs;

// ── Sales Analytics ───────────────────────────────────────────────────────────

/// <summary>Top-level sales analytics response for the admin dashboard.</summary>
/// <param name="TotalOrders">All orders ever placed.</param>
/// <param name="PaidOrders">Successfully paid orders.</param>
/// <param name="PendingOrders">Orders still awaiting payment.</param>
/// <param name="FailedOrders">Orders where payment was declined or failed.</param>
/// <param name="RefundedOrders">Orders that were fully refunded.</param>
/// <param name="OrdersWithDiscount">Paid orders where a coupon discount was applied.</param>
/// <param name="TotalDiscountGiven">Sum of discount amounts across paid orders (mixed currencies — informational).</param>
/// <param name="RevenueByCurrentcy">Revenue grouped by currency — the meaningful breakdown.</param>
/// <param name="RevenueByGateway">Revenue grouped by payment gateway and currency.</param>
/// <param name="RevenueByCountry">Revenue grouped by buyer country and currency.</param>
/// <param name="RevenueByMonth">Monthly revenue trend for the last 12 months, per currency.</param>
public record SalesAnalyticsDto(
    int TotalOrders,
    int PaidOrders,
    int PendingOrders,
    int FailedOrders,
    int RefundedOrders,
    int OrdersWithDiscount,
    decimal TotalDiscountGiven,
    List<CurrencySalesDto> RevenueByCurrentcy,
    List<GatewaySalesDto> RevenueByGateway,
    List<CountrySalesDto> RevenueByCountry,
    List<MonthlySalesDto> RevenueByMonth);

/// <summary>Revenue and order count for a single currency.</summary>
/// <param name="CurrencyCode">ISO 4217 code, e.g. "GBP".</param>
/// <param name="CurrencySymbol">Display symbol, e.g. "£".</param>
/// <param name="TotalRevenue">Sum of AmountPaid for paid orders in this currency.</param>
/// <param name="OrderCount">Number of paid orders in this currency.</param>
/// <param name="AverageOrderValue">TotalRevenue / OrderCount.</param>
public record CurrencySalesDto(
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalRevenue,
    int OrderCount,
    decimal AverageOrderValue);

/// <summary>Revenue and order count for a payment gateway + currency combination.</summary>
/// <param name="Gateway">Gateway name: "PayPal" or "CashFree".</param>
/// <param name="CurrencyCode">ISO 4217 code.</param>
/// <param name="CurrencySymbol">Display symbol.</param>
/// <param name="TotalRevenue">Sum of AmountPaid via this gateway in this currency.</param>
/// <param name="OrderCount">Number of paid orders via this gateway.</param>
public record GatewaySalesDto(
    string Gateway,
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalRevenue,
    int OrderCount);

/// <summary>Revenue and order count for a buyer country + currency combination.</summary>
/// <param name="LocationCode">ISO country code, e.g. "GB", "IN".</param>
/// <param name="CurrencyCode">ISO 4217 code.</param>
/// <param name="CurrencySymbol">Display symbol.</param>
/// <param name="TotalRevenue">Sum of AmountPaid from this country.</param>
/// <param name="OrderCount">Number of paid orders from this country.</param>
public record CountrySalesDto(
    string LocationCode,
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalRevenue,
    int OrderCount);

/// <summary>Revenue for a specific month and currency.</summary>
/// <param name="Year">Calendar year, e.g. 2026.</param>
/// <param name="Month">Calendar month (1–12).</param>
/// <param name="MonthLabel">Human-readable label, e.g. "Mar 2026".</param>
/// <param name="CurrencyCode">ISO 4217 code.</param>
/// <param name="CurrencySymbol">Display symbol.</param>
/// <param name="TotalRevenue">Sum of AmountPaid in this month and currency.</param>
/// <param name="OrderCount">Number of paid orders in this month and currency.</param>
public record MonthlySalesDto(
    int Year,
    int Month,
    string MonthLabel,
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalRevenue,
    int OrderCount);
