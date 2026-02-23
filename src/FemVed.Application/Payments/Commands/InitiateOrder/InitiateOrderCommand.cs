using FemVed.Application.Payments.DTOs;
using MediatR;

namespace FemVed.Application.Payments.Commands.InitiateOrder;

/// <summary>
/// Initiates a purchase order for an authenticated user.
/// Selects CashFree (IN) or PayPal (all other locations) based on the user's country.
/// Idempotent: duplicate <paramref name="IdempotencyKey"/> returns the existing order.
/// </summary>
/// <param name="UserId">Authenticated user's ID.</param>
/// <param name="DurationId">The program duration option to purchase.</param>
/// <param name="CouponCode">Optional discount coupon code.</param>
/// <param name="IdempotencyKey">Client-generated UUID preventing duplicate orders.</param>
public record InitiateOrderCommand(
    Guid UserId,
    Guid DurationId,
    string? CouponCode,
    string IdempotencyKey) : IRequest<InitiateOrderResponse>;
