namespace FemVed.Application.Payments.DTOs;

/// <summary>
/// Represents a refund record as returned to users via <c>GET /api/v1/users/me/refunds</c>.
/// Contains the refund amount, reason, status, and timestamps.
/// </summary>
/// <param name="RefundId">Internal UUID of the refund record.</param>
/// <param name="OrderId">UUID of the order that was refunded.</param>
/// <param name="RefundAmount">Amount refunded (may be partial).</param>
/// <param name="CurrencyCode">ISO 4217 currency code of the refund.</param>
/// <param name="Reason">Human-readable reason provided by the admin. Null if not recorded.</param>
/// <param name="Status">Refund processing state: Pending, Completed, or Failed.</param>
/// <param name="GatewayRefundId">Gateway's own refund reference. Null until processed.</param>
/// <param name="CreatedAt">UTC timestamp when the refund was initiated.</param>
public record RefundDto(
    Guid RefundId,
    Guid OrderId,
    decimal RefundAmount,
    string CurrencyCode,
    string? Reason,
    string Status,
    string? GatewayRefundId,
    DateTimeOffset CreatedAt);
