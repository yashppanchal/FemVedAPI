namespace FemVed.Domain.ValueObjects;

/// <summary>
/// Maps ISO 3166-1 alpha-2 country codes to their ISO 4217 currency code and display symbol.
/// Used to auto-resolve currency details from a location code so callers do not need to supply them.
/// </summary>
public static class CurrencyInfo
{
    private static readonly Dictionary<string, (string Code, string Symbol)> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["IN"] = ("INR", "₹"),
            ["GB"] = ("GBP", "£"),
            ["US"] = ("USD", "$"),
            ["AU"] = ("AUD", "A$"),
            ["AE"] = ("AED", "د.إ"),
            ["CA"] = ("CAD", "CA$"),
            ["SG"] = ("SGD", "S$"),
            ["NZ"] = ("NZD", "NZ$"),
            ["IE"] = ("EUR", "€"),
            ["ZA"] = ("ZAR", "R"),
            ["DE"] = ("EUR", "€"),
            ["NL"] = ("EUR", "€"),
            ["FR"] = ("EUR", "€"),
        };

    /// <summary>
    /// Returns the (CurrencyCode, CurrencySymbol) pair for the given ISO country code,
    /// or <see langword="null"/> if the location code is not in the built-in map.
    /// </summary>
    /// <param name="locationCode">ISO 3166-1 alpha-2 country code, e.g. "GB".</param>
    public static (string Code, string Symbol)? TryGet(string locationCode) =>
        Map.TryGetValue(locationCode, out var info) ? info : null;

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="locationCode"/> is known and
    /// <paramref name="currencyCode"/> matches the expected ISO 4217 code (case-insensitive).
    /// Always returns <see langword="true"/> when the location code is not in the map
    /// (unknown location — caller is responsible for supplying correct values).
    /// </summary>
    /// <param name="locationCode">ISO country code, e.g. "IN".</param>
    /// <param name="currencyCode">ISO 4217 code to validate, e.g. "INR".</param>
    public static bool IsConsistentCode(string locationCode, string currencyCode) =>
        !Map.TryGetValue(locationCode, out var info)
        || string.Equals(info.Code, currencyCode, StringComparison.OrdinalIgnoreCase);
}
