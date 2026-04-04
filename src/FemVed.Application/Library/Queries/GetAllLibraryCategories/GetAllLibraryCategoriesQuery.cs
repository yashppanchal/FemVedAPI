using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetAllLibraryCategories;

/// <summary>Returns all library categories for admin management.</summary>
public record GetAllLibraryCategoriesQuery : IRequest<List<AdminLibraryCategoryDto>>;
