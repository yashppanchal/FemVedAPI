using MediatR;

namespace FemVed.Application.Payments.Events;

/// <summary>
/// Published when a payment gateway confirms an order as FAILED.
/// Subscribers send failure notification emails to the user.
/// </summary>
/// <param name="OrderId">The order that failed.</param>
/// <param name="UserId">The user who attempted the purchase.</param>
public record OrderFailedEvent(Guid OrderId, Guid UserId) : INotification;
