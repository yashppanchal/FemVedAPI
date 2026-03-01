using MediatR;

namespace FemVed.Application.Guided.Commands.UpdateDomain;

/// <summary>
/// Updates an existing guided domain's name, slug, or sort order.
/// AdminOnly operation. All fields are optional — only non-null values are applied.
/// </summary>
/// <param name="DomainId">The domain to update.</param>
/// <param name="Name">New display name (optional).</param>
/// <param name="Slug">New URL slug (optional). Must be unique.</param>
/// <param name="SortOrder">New display order (optional).</param>
/// <param name="AdminUserId">ID of the admin performing the action — written to the audit log.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record UpdateDomainCommand(
    Guid DomainId,
    string? Name,
    string? Slug,
    int? SortOrder,
    Guid AdminUserId,
    string? IpAddress) : IRequest;
