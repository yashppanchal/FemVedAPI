using FemVed.Application.Guided.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Queries.GetCategoryBySlug;

/// <summary>
/// Handles <see cref="GetCategoryBySlugQuery"/>.
/// Returns the category with its published programs for the given URL slug.
/// </summary>
public sealed class GetCategoryBySlugQueryHandler : IRequestHandler<GetCategoryBySlugQuery, GuidedCategoryDto>
{
    private readonly IGuidedCatalogReadService _readService;
    private readonly ILogger<GetCategoryBySlugQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetCategoryBySlugQueryHandler(
        IGuidedCatalogReadService readService,
        ILogger<GetCategoryBySlugQueryHandler> logger)
    {
        _readService = readService;
        _logger = logger;
    }

    /// <summary>Returns the category DTO for the requested slug.</summary>
    /// <param name="request">The query containing the slug and location code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Category DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the slug does not match an active category.</exception>
    public async Task<GuidedCategoryDto> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching category by slug {Slug} for location {LocationCode}", request.Slug, request.LocationCode);

        var result = await _readService.GetCategoryBySlugAsync(request.Slug, request.LocationCode, cancellationToken);
        if (result is null)
            throw new NotFoundException("GuidedCategory", request.Slug);

        _logger.LogInformation("Category {Slug} returned successfully", request.Slug);
        return result;
    }
}
