using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetLibraryCategoryBySlug;

/// <summary>
/// Handles <see cref="GetLibraryCategoryBySlugQuery"/>.
/// Returns the library category with its published videos for the given URL slug.
/// </summary>
public sealed class GetLibraryCategoryBySlugQueryHandler
    : IRequestHandler<GetLibraryCategoryBySlugQuery, LibraryCategoryDto>
{
    private readonly ILibraryCatalogReadService _readService;
    private readonly ILogger<GetLibraryCategoryBySlugQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetLibraryCategoryBySlugQueryHandler(
        ILibraryCatalogReadService readService,
        ILogger<GetLibraryCategoryBySlugQueryHandler> logger)
    {
        _readService = readService;
        _logger = logger;
    }

    /// <summary>Returns the library category DTO for the requested slug.</summary>
    /// <param name="request">The query containing the slug and location code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the slug does not match an active category.</exception>
    public async Task<LibraryCategoryDto> Handle(
        GetLibraryCategoryBySlugQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching library category by slug {Slug} for location {LocationCode}",
            request.Slug, request.LocationCode);

        var result = await _readService.GetCategoryBySlugAsync(request.Slug, request.LocationCode, cancellationToken);
        if (result is null)
            throw new NotFoundException("LibraryCategory", request.Slug);

        _logger.LogInformation("Library category {Slug} returned successfully", request.Slug);
        return result;
    }
}
