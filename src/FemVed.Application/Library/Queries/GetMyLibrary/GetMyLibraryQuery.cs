using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetMyLibrary;

/// <summary>
/// Returns the authenticated user's purchased library videos with watch progress.
/// </summary>
/// <param name="UserId">Authenticated user's ID.</param>
public record GetMyLibraryQuery(Guid UserId) : IRequest<MyLibraryResponse>;
