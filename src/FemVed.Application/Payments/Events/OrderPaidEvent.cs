using FemVed.Domain.Enums;
using MediatR;

namespace FemVed.Application.Payments.Events;

/// <summary>
/// Published when a payment gateway confirms an order as PAID.
/// Subscribers create user access records and send post-purchase notifications.
/// Supports both guided (program) and library (video) purchases.
/// </summary>
/// <param name="OrderId">The order that was paid.</param>
/// <param name="UserId">The user who purchased.</param>
/// <param name="OrderSource">Which domain module: GUIDED or LIBRARY.</param>
/// <param name="ProgramId">The program purchased (guided only, null for library).</param>
/// <param name="DurationId">The specific duration option purchased (guided only, null for library).</param>
/// <param name="ExpertId">The expert who owns the content.</param>
/// <param name="VideoId">The library video purchased (library only, null for guided).</param>
public record OrderPaidEvent(
    Guid OrderId,
    Guid UserId,
    OrderSource OrderSource,
    Guid? ProgramId,
    Guid? DurationId,
    Guid ExpertId,
    Guid? VideoId = null) : INotification;
