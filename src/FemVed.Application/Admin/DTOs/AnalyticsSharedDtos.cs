namespace FemVed.Application.Admin.DTOs;

/// <summary>A revenue or amount figure in a specific currency, used across analytics DTOs.</summary>
/// <param name="CurrencyCode">ISO 4217 code, e.g. "GBP".</param>
/// <param name="CurrencySymbol">Display symbol, e.g. "£".</param>
/// <param name="Amount">Monetary amount in this currency.</param>
/// <param name="OrderCount">Number of orders contributing to this amount (0 when used for payouts).</param>
public record CurrencyAmountDto(
    string CurrencyCode,
    string CurrencySymbol,
    decimal Amount,
    int OrderCount = 0);
