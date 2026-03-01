namespace FemVed.Domain.Enums;

/// <summary>
/// ISO 3166-1 alpha-2 country codes supported by the FemVed platform.
/// Used for:
///   - Payment gateway selection (IN → CashFree/INR, everything else → PayPal)
///   - Currency/price display in the guided catalog
///   - ?country_code query parameter on public GET endpoints
///
/// Price fallback order in the catalog: exact match → GB → any available price → "N/A".
/// To add pricing for a new country, insert a row in <c>duration_prices</c>
/// with the matching <c>location_code</c> string (and update the DB CHECK constraint if present).
/// </summary>
public enum LocationCode
{
    // ── Tier 1 — full pricing support (rows exist in duration_prices) ─────────

    /// <summary>India — INR (₹) — payment via CashFree.</summary>
    IN,

    /// <summary>United Kingdom — GBP (£) — payment via PayPal. Default fallback.</summary>
    GB,

    /// <summary>United States — USD ($) — payment via PayPal.</summary>
    US,

    // ── Tier 2 — supported dial codes (CLAUDE.md §8); add pricing rows to unlock ──

    /// <summary>Australia — AUD (A$) — payment via PayPal.</summary>
    AU,

    /// <summary>United Arab Emirates — AED (د.إ) — payment via PayPal.</summary>
    AE,

    // ── Tier 3 — common diaspora / expansion markets ─────────────────────────

    /// <summary>Canada — CAD (CA$) — payment via PayPal.</summary>
    CA,

    /// <summary>Singapore — SGD (S$) — payment via PayPal.</summary>
    SG,

    /// <summary>New Zealand — NZD (NZ$) — payment via PayPal.</summary>
    NZ,

    /// <summary>Ireland — EUR (€) — payment via PayPal.</summary>
    IE,

    /// <summary>South Africa — ZAR (R) — payment via PayPal.</summary>
    ZA,

    /// <summary>Germany — EUR (€) — payment via PayPal.</summary>
    DE,

    /// <summary>Netherlands — EUR (€) — payment via PayPal.</summary>
    NL,

    /// <summary>France — EUR (€) — payment via PayPal.</summary>
    FR,
}
