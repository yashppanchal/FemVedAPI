using MediatR;

namespace FemVed.Application.Guided.Commands.DeleteCategory;

/// <summary>
/// Soft-deletes a guided category (sets IsDeleted = true, IsActive = false).
/// AdminOnly operation.
/// </summary>
/// <param name="CategoryId">The category to soft-delete.</param>
/// <param name="AdminUserId">ID of the admin performing the action — written to the audit log.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record DeleteCategoryCommand(Guid CategoryId, Guid AdminUserId, string? IpAddress) : IRequest;
