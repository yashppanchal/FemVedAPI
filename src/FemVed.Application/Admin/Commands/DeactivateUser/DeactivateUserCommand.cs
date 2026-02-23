using MediatR;

namespace FemVed.Application.Admin.Commands.DeactivateUser;

/// <summary>Deactivates a user account (sets IsActive = false). The user can no longer log in.</summary>
/// <param name="TargetUserId">UUID of the user to deactivate.</param>
/// <param name="AdminUserId">UUID of the Admin performing the action (for audit log).</param>
/// <param name="IpAddress">Client IP address (for audit log).</param>
public record DeactivateUserCommand(Guid TargetUserId, Guid AdminUserId, string? IpAddress) : IRequest;
