using MediatR;

namespace FemVed.Application.Payments.Commands.CancelOrder;

/// <summary>
/// Cancels a pending order initiated by the authenticated user.
/// Only orders in <c>Pending</c> status may be cancelled.
/// </summary>
/// <param name="OrderId">UUID of the order to cancel.</param>
/// <param name="UserId">Authenticated user's ID (ownership is verified inside the handler).</param>
public record CancelOrderCommand(Guid OrderId, Guid UserId) : IRequest;
