using MediatR;

namespace FemVed.Application.Admin.Commands.ChangeUserRole;

/// <summary>Changes the role of a user account (e.g. User → Expert, User → Admin).</summary>
/// <param name="TargetUserId">UUID of the user whose role will change.</param>
/// <param name="RoleId">New role ID: 1 = Admin, 2 = Expert, 3 = User.</param>
/// <param name="AdminUserId">UUID of the Admin performing the action (for audit log).</param>
/// <param name="IpAddress">Client IP address (for audit log).</param>
public record ChangeUserRoleCommand(Guid TargetUserId, short RoleId, Guid AdminUserId, string? IpAddress) : IRequest;
