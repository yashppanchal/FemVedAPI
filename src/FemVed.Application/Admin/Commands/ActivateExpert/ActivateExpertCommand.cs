using MediatR;

namespace FemVed.Application.Admin.Commands.ActivateExpert;

/// <summary>Makes an expert profile visible in the catalog (sets IsActive = true).</summary>
/// <param name="ExpertId">UUID of the expert to activate.</param>
/// <param name="AdminUserId">UUID of the Admin performing the action (for audit log).</param>
/// <param name="IpAddress">Client IP address (for audit log).</param>
public record ActivateExpertCommand(Guid ExpertId, Guid AdminUserId, string? IpAddress) : IRequest;
