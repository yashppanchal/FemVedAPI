using MediatR;

namespace FemVed.Application.Payments.Events;

/// <summary>
/// Published when a payment gateway confirms an order as PAID.
/// Subscribers create user access records and send post-purchase notifications.
/// </summary>
/// <param name="OrderId">The order that was paid.</param>
/// <param name="UserId">The user who purchased.</param>
/// <param name="ProgramId">The program purchased.</param>
/// <param name="DurationId">The specific duration option purchased.</param>
/// <param name="ExpertId">The expert who owns the program.</param>
public record OrderPaidEvent(
    Guid OrderId,
    Guid UserId,
    Guid ProgramId,
    Guid DurationId,
    Guid ExpertId) : INotification;
