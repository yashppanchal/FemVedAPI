using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Commands.ChangeUserEmail;

/// <summary>
/// Changes the email address of any user account. Admin only.
/// Validates the new email is not already in use, updates the user record,
/// and writes an audit log entry.
/// </summary>
/// <param name="TargetUserId">The user whose email will be changed.</param>
/// <param name="NewEmail">The new email address.</param>
/// <param name="AdminUserId">Admin performing the action — written to the audit log.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record ChangeUserEmailCommand(
    Guid TargetUserId,
    string NewEmail,
    Guid AdminUserId,
    string? IpAddress) : IRequest<AdminUserDto>;
