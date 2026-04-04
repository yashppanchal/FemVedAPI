using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetAllLibraryFilterTypes;

/// <summary>Returns all library filter types for admin management.</summary>
public record GetAllLibraryFilterTypesQuery : IRequest<List<AdminFilterTypeDto>>;
