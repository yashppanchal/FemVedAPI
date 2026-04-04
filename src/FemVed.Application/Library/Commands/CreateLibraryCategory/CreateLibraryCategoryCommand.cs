using MediatR;

namespace FemVed.Application.Library.Commands.CreateLibraryCategory;

/// <summary>
/// Creates a new library category within a domain.
/// AdminOnly operation.
/// </summary>
/// <param name="DomainId">The domain this category belongs to.</param>
/// <param name="Name">Display name, e.g. "Hormonal Health Support".</param>
/// <param name="Slug">Unique URL slug, e.g. "hormonal-health-support".</param>
/// <param name="Description">Optional description of this category.</param>
/// <param name="CardImage">Card image URL for this category (optional).</param>
/// <param name="SortOrder">Display ordering within the domain.</param>
public record CreateLibraryCategoryCommand(
    Guid DomainId,
    string Name,
    string Slug,
    string? Description,
    string? CardImage,
    int SortOrder) : IRequest<Guid>;
