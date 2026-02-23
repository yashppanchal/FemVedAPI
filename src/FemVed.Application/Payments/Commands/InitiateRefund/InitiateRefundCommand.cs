using MediatR;

namespace FemVed.Application.Payments.Commands.InitiateRefund;

/// <summary>
/// Initiates a refund for a fully paid order. Admin only.
/// Creates a <see cref="Domain.Entities.Refund"/> record and calls the payment gateway.
/// </summary>
/// <param name="OrderId">The order to refund.</param>
/// <param name="InitiatedByUserId">Admin user ID who authorised the refund.</param>
/// <param name="RefundAmount">Amount to refund (must not exceed the original AmountPaid).</param>
/// <param name="Reason">Human-readable reason for the refund.</param>
public record InitiateRefundCommand(
    Guid OrderId,
    Guid InitiatedByUserId,
    decimal RefundAmount,
    string Reason) : IRequest;
