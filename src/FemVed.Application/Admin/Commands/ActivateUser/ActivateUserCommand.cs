using MediatR;

namespace FemVed.Application.Admin.Commands.ActivateUser;

/// <summary>Re-activates a previously deactivated user account (sets IsActive = true).</summary>
/// <param name="TargetUserId">UUID of the user to activate.</param>
/// <param name="AdminUserId">UUID of the Admin performing the action (for audit log).</param>
/// <param name="IpAddress">Client IP address (for audit log).</param>
public record ActivateUserCommand(Guid TargetUserId, Guid AdminUserId, string? IpAddress) : IRequest;
