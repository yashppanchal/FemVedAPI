using MediatR;

namespace FemVed.Application.Library.Commands.DeleteLibraryCategory;

/// <summary>
/// Soft-deactivates a library category (sets IsActive = false).
/// AdminOnly operation.
/// </summary>
/// <param name="CategoryId">The category to deactivate.</param>
public record DeleteLibraryCategoryCommand(Guid CategoryId) : IRequest;
