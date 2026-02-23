namespace FemVed.Domain.Utilities;

/// <summary>
/// Maps telephone dial codes to ISO 3166-1 alpha-2 country codes.
/// Used to derive <c>country_iso_code</c> from the user-supplied <c>country_dial_code</c> at registration.
/// The ISO code then drives payment gateway and currency selection.
/// </summary>
public static class DialCodeMapper
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        { "+91",  "IN" },   // India        → CashFree / INR
        { "+44",  "GB" },   // United Kingdom → PayPal / GBP
        { "+1",   "US" },   // United States  → PayPal / USD
        { "+61",  "AU" },   // Australia      → PayPal / AUD
        { "+971", "AE" },   // UAE            → PayPal / AED
        { "+64",  "NZ" },   // New Zealand    → PayPal / NZD
        { "+353", "IE" },   // Ireland        → PayPal / EUR
        { "+49",  "DE" },   // Germany        → PayPal / EUR
        { "+33",  "FR" },   // France         → PayPal / EUR
        { "+31",  "NL" },   // Netherlands    → PayPal / EUR
        { "+65",  "SG" },   // Singapore      → PayPal / SGD
        { "+60",  "MY" },   // Malaysia       → PayPal / MYR
        { "+27",  "ZA" },   // South Africa   → PayPal / ZAR
        { "+94",  "LK" },   // Sri Lanka      → PayPal / LKR
    };

    /// <summary>
    /// Converts a dial code to an ISO country code.
    /// Returns "GB" as the default if the dial code is unrecognised (safe default for PayPal/GBP).
    /// </summary>
    /// <param name="dialCode">Dial code including leading +, e.g. "+44".</param>
    /// <returns>ISO 3166-1 alpha-2 code, e.g. "GB".</returns>
    public static string ToIsoCode(string? dialCode)
    {
        if (string.IsNullOrWhiteSpace(dialCode))
            return "GB";

        return Map.TryGetValue(dialCode.Trim(), out var iso) ? iso : "GB";
    }

    /// <summary>
    /// Attempts to resolve a dial code to an ISO code, returning false if unrecognised.
    /// </summary>
    /// <param name="dialCode">Dial code including leading +.</param>
    /// <param name="isoCode">Resolved ISO code, or null if not found.</param>
    /// <returns>True if the dial code was found in the mapping table.</returns>
    public static bool TryGetIsoCode(string? dialCode, out string? isoCode)
    {
        isoCode = null;
        if (string.IsNullOrWhiteSpace(dialCode)) return false;
        return Map.TryGetValue(dialCode.Trim(), out isoCode);
    }
}
