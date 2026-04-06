namespace FemVed.Application.Library.DTOs;

// ── Library Analytics DTOs ───────────────────────────────────────────────────

/// <summary>Top-level analytics response for GET /api/v1/admin/library/analytics.</summary>
/// <param name="TotalVideos">All videos (any status).</param>
/// <param name="PublishedVideos">Videos with status PUBLISHED.</param>
/// <param name="DraftVideos">Videos with status DRAFT.</param>
/// <param name="ArchivedVideos">Videos with status ARCHIVED.</param>
/// <param name="TotalPurchases">Number of UserLibraryAccess records (successful purchases).</param>
/// <param name="TotalOrders">Library orders (any status).</param>
/// <param name="PaidOrders">Library orders with status Paid.</param>
/// <param name="FailedOrders">Library orders with status Failed.</param>
/// <param name="RevenueByCurrency">Library revenue grouped by currency.</param>
/// <param name="TopSellingVideos">Top 10 videos by purchase count.</param>
/// <param name="RevenueByMonth">Monthly library revenue for the last 12 months.</param>
public record LibraryAnalyticsDto(
    int TotalVideos,
    int PublishedVideos,
    int DraftVideos,
    int ArchivedVideos,
    int TotalPurchases,
    int TotalOrders,
    int PaidOrders,
    int FailedOrders,
    List<LibraryCurrencyRevenueDto> RevenueByCurrency,
    List<LibraryTopVideoDto> TopSellingVideos,
    List<LibraryMonthlyRevenueDto> RevenueByMonth);

/// <summary>Revenue in a single currency for library orders.</summary>
/// <param name="CurrencyCode">ISO 4217 code.</param>
/// <param name="CurrencySymbol">Display symbol.</param>
/// <param name="TotalRevenue">Sum of paid amounts.</param>
/// <param name="OrderCount">Number of paid orders.</param>
public record LibraryCurrencyRevenueDto(
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalRevenue,
    int OrderCount);

/// <summary>A top-selling library video.</summary>
/// <param name="VideoId">Video primary key.</param>
/// <param name="Title">Video title.</param>
/// <param name="VideoType">MASTERCLASS or SERIES.</param>
/// <param name="PurchaseCount">Number of purchases.</param>
/// <param name="ExpertName">Expert display name.</param>
public record LibraryTopVideoDto(
    Guid VideoId,
    string Title,
    string VideoType,
    int PurchaseCount,
    string ExpertName);

/// <summary>Monthly library revenue for a specific currency.</summary>
/// <param name="Year">Calendar year.</param>
/// <param name="Month">Calendar month 1–12.</param>
/// <param name="MonthLabel">E.g. "Apr 2026".</param>
/// <param name="CurrencyCode">ISO 4217 code.</param>
/// <param name="CurrencySymbol">Display symbol.</param>
/// <param name="TotalRevenue">Sum of paid amounts.</param>
/// <param name="OrderCount">Number of paid orders.</param>
public record LibraryMonthlyRevenueDto(
    int Year,
    int Month,
    string MonthLabel,
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalRevenue,
    int OrderCount);

// ── Library Purchases list ───────────────────────────────────────────────────

/// <summary>Response for GET /api/v1/admin/library/purchases — who bought what.</summary>
/// <param name="Purchases">Flat list of all library purchases.</param>
public record LibraryPurchasesResponse(List<LibraryPurchaseDto> Purchases);

/// <summary>A single library purchase record.</summary>
/// <param name="AccessId">UserLibraryAccess primary key.</param>
/// <param name="UserId">Buyer user ID.</param>
/// <param name="UserName">Buyer display name.</param>
/// <param name="UserEmail">Buyer email.</param>
/// <param name="VideoId">Video primary key.</param>
/// <param name="VideoTitle">Video title.</param>
/// <param name="VideoType">MASTERCLASS or SERIES.</param>
/// <param name="OrderId">Originating order ID.</param>
/// <param name="AmountPaid">Amount paid for this order.</param>
/// <param name="CurrencyCode">ISO 4217 code.</param>
/// <param name="CurrencySymbol">Display symbol.</param>
/// <param name="PurchasedAt">UTC purchase timestamp.</param>
/// <param name="IsActive">Whether access is currently active.</param>
public record LibraryPurchaseDto(
    Guid AccessId,
    Guid UserId,
    string UserName,
    string UserEmail,
    Guid VideoId,
    string VideoTitle,
    string VideoType,
    Guid OrderId,
    decimal AmountPaid,
    string CurrencyCode,
    string CurrencySymbol,
    DateTimeOffset PurchasedAt,
    bool IsActive);
