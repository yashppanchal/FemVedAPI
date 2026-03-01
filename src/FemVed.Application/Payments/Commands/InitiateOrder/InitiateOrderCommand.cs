using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Enums;
using MediatR;

namespace FemVed.Application.Payments.Commands.InitiateOrder;

/// <summary>
/// Initiates a purchase order for an authenticated user.
/// Location resolution: <paramref name="CountryCode"/> if provided, otherwise default "GB".
/// Gateway resolution: <paramref name="Gateway"/> if provided, otherwise default PayPal.
/// Any country + gateway combination is valid; the frontend is in full control.
/// Idempotent: duplicate <paramref name="IdempotencyKey"/> returns the existing order.
/// </summary>
/// <param name="UserId">Authenticated user's ID.</param>
/// <param name="DurationId">The program duration option to purchase.</param>
/// <param name="CouponCode">Optional discount coupon code.</param>
/// <param name="IdempotencyKey">Client-generated UUID preventing duplicate orders.</param>
/// <param name="CountryCode">
/// Optional. Selects the price tier for this purchase. Defaults to "GB" when omitted.
/// </param>
/// <param name="Gateway">
/// Optional. Selects the payment gateway. Defaults to PayPal when omitted.
/// Any country + gateway combination is accepted.
/// </param>
public record InitiateOrderCommand(
    Guid UserId,
    Guid DurationId,
    string? CouponCode,
    string IdempotencyKey,
    LocationCode? CountryCode = null,
    PaymentGateway? Gateway = null) : IRequest<InitiateOrderResponse>;
