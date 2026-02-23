using FemVed.Application.Guided.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Queries.GetProgramBySlug;

/// <summary>
/// Handles <see cref="GetProgramBySlugQuery"/>.
/// Returns a published program with full detail for the given URL slug.
/// </summary>
public sealed class GetProgramBySlugQueryHandler : IRequestHandler<GetProgramBySlugQuery, ProgramInCategoryDto>
{
    private readonly IGuidedCatalogReadService _readService;
    private readonly ILogger<GetProgramBySlugQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetProgramBySlugQueryHandler(
        IGuidedCatalogReadService readService,
        ILogger<GetProgramBySlugQueryHandler> logger)
    {
        _readService = readService;
        _logger = logger;
    }

    /// <summary>Returns the program DTO for the requested slug.</summary>
    /// <param name="request">The query containing the slug and location code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Program DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the slug does not match a published program.</exception>
    public async Task<ProgramInCategoryDto> Handle(GetProgramBySlugQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching program by slug {Slug} for location {LocationCode}", request.Slug, request.LocationCode);

        var result = await _readService.GetProgramBySlugAsync(request.Slug, request.LocationCode, cancellationToken);
        if (result is null)
            throw new NotFoundException("Program", request.Slug);

        _logger.LogInformation("Program {Slug} returned successfully", request.Slug);
        return result;
    }
}
