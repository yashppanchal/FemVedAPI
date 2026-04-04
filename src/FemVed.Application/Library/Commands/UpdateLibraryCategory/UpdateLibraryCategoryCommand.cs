using MediatR;

namespace FemVed.Application.Library.Commands.UpdateLibraryCategory;

/// <summary>
/// Updates an existing library category. All fields except CategoryId are optional --
/// only non-null values are applied.
/// AdminOnly operation.
/// </summary>
/// <param name="CategoryId">The category to update.</param>
/// <param name="Name">New display name (optional).</param>
/// <param name="Slug">New URL slug (optional). Must be unique.</param>
/// <param name="Description">New description (optional).</param>
/// <param name="CardImage">New card image URL (optional).</param>
/// <param name="SortOrder">New display order (optional).</param>
/// <param name="IsActive">New active status (optional).</param>
public record UpdateLibraryCategoryCommand(
    Guid CategoryId,
    string? Name,
    string? Slug,
    string? Description,
    string? CardImage,
    int? SortOrder,
    bool? IsActive) : IRequest;
