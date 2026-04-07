namespace FemVed.Application.Library.DTOs;

/// <summary>Response DTO for expert library video sales overview.</summary>
public record ExpertLibrarySalesResponse(
    int TotalVideos,
    int TotalPurchases,
    List<ExpertLibraryVideoSalesDto> Videos,
    List<ExpertLibrarySalesCurrencyDto> RevenueByurrency);

/// <summary>Per-video sales summary for the expert.</summary>
public record ExpertLibraryVideoSalesDto(
    Guid VideoId,
    string Title,
    string VideoType,
    string Status,
    int PurchaseCount,
    DateTimeOffset CreatedAt);

/// <summary>Revenue per currency for the expert's library sales.</summary>
public record ExpertLibrarySalesCurrencyDto(
    string CurrencyCode,
    string CurrencySymbol,
    decimal TotalRevenue,
    int OrderCount);

/// <summary>Individual purchase record for the expert's videos.</summary>
public record ExpertLibraryPurchaseDto(
    string BuyerName,
    string BuyerEmail,
    string VideoTitle,
    decimal AmountPaid,
    string CurrencyCode,
    string CurrencySymbol,
    DateTimeOffset PurchasedAt);
