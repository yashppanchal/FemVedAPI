namespace FemVed.Application.Admin.DTOs;

// ── Expert Payout DTOs ────────────────────────────────────────────────────────

/// <summary>
/// Balance sheet for a single expert showing gross revenue, expert share,
/// platform commission, total paid out, and outstanding balance.
/// </summary>
/// <param name="ExpertId">UUID of the expert profile.</param>
/// <param name="ExpertName">Display name.</param>
/// <param name="CommissionRate">Expert's revenue share %, e.g. 80.00.</param>
/// <param name="TotalEarned">Gross revenue from all paid orders for this expert's programs.</param>
/// <param name="ExpertShare">CommissionRate % of TotalEarned — what the expert is owed.</param>
/// <param name="PlatformCommission">(100 - CommissionRate) % of TotalEarned — platform's cut.</param>
/// <param name="TotalPaid">Amounts already paid to this expert (from expert_payouts records).</param>
/// <param name="OutstandingBalance">ExpertShare minus TotalPaid per currency (may be negative if overpaid).</param>
/// <param name="LastPayoutAt">UTC timestamp of the most recent payout, or null if never paid.</param>
public record ExpertPayoutBalanceDto(
    Guid ExpertId,
    string ExpertName,
    decimal CommissionRate,
    List<CurrencyAmountDto> TotalEarned,
    List<CurrencyAmountDto> ExpertShare,
    List<CurrencyAmountDto> PlatformCommission,
    List<CurrencyAmountDto> TotalPaid,
    List<CurrencyAmountDto> OutstandingBalance,
    DateTimeOffset? LastPayoutAt);

/// <summary>A single recorded payout to an expert.</summary>
/// <param name="PayoutId">UUID of the payout record.</param>
/// <param name="ExpertId">UUID of the expert profile.</param>
/// <param name="ExpertName">Expert display name.</param>
/// <param name="Amount">Amount paid in this transaction.</param>
/// <param name="CurrencyCode">ISO 4217 code, e.g. "GBP".</param>
/// <param name="CurrencySymbol">Display symbol, e.g. "£".</param>
/// <param name="PaymentReference">Bank wire ref, PayPal transaction ID, etc. Null if not recorded.</param>
/// <param name="Notes">Admin notes about this payment. Null if not recorded.</param>
/// <param name="PaidBy">UUID of the admin who recorded this payout.</param>
/// <param name="PaidByName">Full name of the admin who recorded this payout.</param>
/// <param name="PaidAt">UTC timestamp when funds were transferred.</param>
/// <param name="CreatedAt">UTC timestamp when this record was created.</param>
public record ExpertPayoutRecordDto(
    Guid PayoutId,
    Guid ExpertId,
    string ExpertName,
    decimal Amount,
    string CurrencyCode,
    string CurrencySymbol,
    string? PaymentReference,
    string? Notes,
    Guid PaidBy,
    string PaidByName,
    DateTimeOffset PaidAt,
    DateTimeOffset CreatedAt);
