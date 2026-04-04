using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetMyLibrary;

/// <summary>
/// Handles <see cref="GetMyLibraryQuery"/>.
/// Delegates to <see cref="ILibraryCatalogReadService"/> for the EF Core projection.
/// </summary>
public sealed class GetMyLibraryQueryHandler
    : IRequestHandler<GetMyLibraryQuery, MyLibraryResponse>
{
    private readonly ILibraryCatalogReadService _readService;

    /// <summary>Initialises the handler with the read service.</summary>
    public GetMyLibraryQueryHandler(ILibraryCatalogReadService readService)
    {
        _readService = readService;
    }

    /// <summary>Returns the user's purchased library videos.</summary>
    /// <param name="request">The query containing the user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>My library response with purchased videos.</returns>
    public async Task<MyLibraryResponse> Handle(
        GetMyLibraryQuery request,
        CancellationToken cancellationToken)
    {
        return await _readService.GetMyLibraryAsync(request.UserId, cancellationToken);
    }
}
