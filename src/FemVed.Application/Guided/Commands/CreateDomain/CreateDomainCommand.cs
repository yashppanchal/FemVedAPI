using MediatR;

namespace FemVed.Application.Guided.Commands.CreateDomain;

/// <summary>
/// Creates a new guided domain (e.g. "Guided 1:1 Care").
/// AdminOnly operation.
/// </summary>
/// <param name="Name">Display name shown in UI navigation.</param>
/// <param name="Slug">URL slug — must be unique, e.g. "guided-1-1-care".</param>
/// <param name="SortOrder">Display ordering (ascending). Defaults to 0.</param>
/// <param name="AdminUserId">ID of the admin performing the action — written to the audit log.</param>
/// <param name="IpAddress">Client IP address — written to the audit log.</param>
public record CreateDomainCommand(
    string Name,
    string Slug,
    int SortOrder,
    Guid AdminUserId,
    string? IpAddress) : IRequest<Guid>;
