namespace FemVed.Application.Guided.DTOs;

/// <summary>
/// Management view of a single location-specific price row — all fields, all locations.
/// Used in expert / admin dashboard responses (unlike the public tree which filters by one location).
/// </summary>
/// <param name="PriceId">Primary key of the <c>duration_prices</c> row.</param>
/// <param name="LocationCode">ISO country code, e.g. "GB", "IN", "US".</param>
/// <param name="Amount">Price amount, e.g. 320.00.</param>
/// <param name="CurrencyCode">ISO 4217 code, e.g. "GBP".</param>
/// <param name="CurrencySymbol">Display symbol, e.g. "£".</param>
/// <param name="IsActive">Whether this price is currently active.</param>
public record DurationPriceManagementDto(
    Guid PriceId,
    string LocationCode,
    decimal Amount,
    string CurrencyCode,
    string CurrencySymbol,
    bool IsActive);

/// <summary>
/// Management view of a program duration, including all its location-specific prices.
/// Used in expert / admin dashboard responses.
/// </summary>
/// <param name="DurationId">Primary key of the <c>program_durations</c> row.</param>
/// <param name="Label">Human-readable label, e.g. "6 weeks".</param>
/// <param name="Weeks">Number of weeks.</param>
/// <param name="SortOrder">Display ordering (ascending).</param>
/// <param name="IsActive">Whether this duration is currently active.</param>
/// <param name="Prices">All location-specific prices for this duration (all countries).</param>
public record DurationManagementDto(
    Guid DurationId,
    string Label,
    short Weeks,
    int SortOrder,
    bool IsActive,
    List<DurationPriceManagementDto> Prices);
