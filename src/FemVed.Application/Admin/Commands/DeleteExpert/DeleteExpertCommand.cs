using MediatR;

namespace FemVed.Application.Admin.Commands.DeleteExpert;

/// <summary>Soft-deletes an expert profile (sets IsDeleted = true, IsActive = false). Never hard-deletes.</summary>
/// <param name="ExpertId">UUID of the expert profile to soft-delete.</param>
/// <param name="AdminUserId">UUID of the Admin performing the action (for audit log).</param>
/// <param name="IpAddress">Client IP address (for audit log).</param>
public record DeleteExpertCommand(Guid ExpertId, Guid AdminUserId, string? IpAddress) : IRequest;
