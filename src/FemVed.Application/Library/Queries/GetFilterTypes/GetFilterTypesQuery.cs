using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetFilterTypes;

/// <summary>
/// Returns all active filter tabs for the Wellness Library catalog page.
/// Includes a synthetic "All Programs" entry at position 0.
/// </summary>
public record GetFilterTypesQuery : IRequest<List<LibraryFilterDto>>;
