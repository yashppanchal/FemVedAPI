using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Commands.RecordExpertPayout;

/// <summary>
/// Records a payment made from the platform to an expert.
/// Each call creates one row in <c>expert_payouts</c>.
/// The outstanding balance is always computed dynamically from sales minus payouts.
/// </summary>
/// <param name="ExpertId">UUID of the expert receiving the payment.</param>
/// <param name="Amount">Amount transferred. Must be greater than 0.</param>
/// <param name="CurrencyCode">ISO 4217 code matching the currency of the transfer, e.g. "GBP".</param>
/// <param name="PaidAt">UTC timestamp when the funds were actually transferred.</param>
/// <param name="PaymentReference">Optional bank wire ref, PayPal transaction ID, etc.</param>
/// <param name="Notes">Optional admin notes about this payment.</param>
/// <param name="AdminUserId">Admin recording this payout — written to audit log.</param>
/// <param name="IpAddress">Client IP address — written to audit log.</param>
public record RecordExpertPayoutCommand(
    Guid ExpertId,
    decimal Amount,
    string CurrencyCode,
    DateTimeOffset PaidAt,
    string? PaymentReference,
    string? Notes,
    Guid AdminUserId,
    string? IpAddress) : IRequest<ExpertPayoutRecordDto>;
