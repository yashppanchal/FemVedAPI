using MediatR;

namespace FemVed.Application.Admin.Commands.DeleteUser;

/// <summary>Soft-deletes a user account (sets IsDeleted = true). Never hard-deletes.</summary>
/// <param name="TargetUserId">UUID of the user to soft-delete.</param>
/// <param name="AdminUserId">UUID of the Admin performing the action (for audit log).</param>
/// <param name="IpAddress">Client IP address (for audit log).</param>
public record DeleteUserCommand(Guid TargetUserId, Guid AdminUserId, string? IpAddress) : IRequest;
