using MediatR;

namespace FemVed.Application.Guided.Commands.DeleteDomain;

/// <summary>
/// Soft-deletes a guided domain (sets IsDeleted = true, IsActive = false).
/// AdminOnly operation.
/// </summary>
/// <param name="DomainId">The domain to soft-delete.</param>
/// <param name="AdminUserId">ID of the admin performing the action — written to the audit log.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record DeleteDomainCommand(Guid DomainId, Guid AdminUserId, string? IpAddress) : IRequest;
