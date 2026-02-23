using MediatR;

namespace FemVed.Application.Admin.Commands.DeactivateExpert;

/// <summary>Hides an expert profile from the catalog (sets IsActive = false).</summary>
/// <param name="ExpertId">UUID of the expert to deactivate.</param>
/// <param name="AdminUserId">UUID of the Admin performing the action (for audit log).</param>
/// <param name="IpAddress">Client IP address (for audit log).</param>
public record DeactivateExpertCommand(Guid ExpertId, Guid AdminUserId, string? IpAddress) : IRequest;
