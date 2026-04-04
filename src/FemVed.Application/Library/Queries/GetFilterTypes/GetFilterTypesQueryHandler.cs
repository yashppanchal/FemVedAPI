using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetFilterTypes;

/// <summary>
/// Handles <see cref="GetFilterTypesQuery"/>.
/// Returns the dynamic filter tabs from the database.
/// </summary>
public sealed class GetFilterTypesQueryHandler
    : IRequestHandler<GetFilterTypesQuery, List<LibraryFilterDto>>
{
    private readonly ILibraryCatalogReadService _readService;
    private readonly ILogger<GetFilterTypesQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetFilterTypesQueryHandler(
        ILibraryCatalogReadService readService,
        ILogger<GetFilterTypesQueryHandler> logger)
    {
        _readService = readService;
        _logger = logger;
    }

    /// <summary>Returns the list of filter tabs.</summary>
    /// <param name="request">The query (no parameters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of filter DTOs.</returns>
    public async Task<List<LibraryFilterDto>> Handle(
        GetFilterTypesQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching library filter types");

        var result = await _readService.GetFilterTypesAsync(cancellationToken);

        _logger.LogInformation("Returned {Count} library filter types", result.Count);
        return result;
    }
}
