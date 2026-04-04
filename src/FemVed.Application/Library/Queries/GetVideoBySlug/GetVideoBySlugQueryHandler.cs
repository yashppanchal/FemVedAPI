using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetVideoBySlug;

/// <summary>
/// Handles <see cref="GetVideoBySlugQuery"/>.
/// Returns the video detail page data — trailer URL included, stream URL NEVER included.
/// </summary>
public sealed class GetVideoBySlugQueryHandler
    : IRequestHandler<GetVideoBySlugQuery, LibraryVideoDetailResponse>
{
    private readonly ILibraryCatalogReadService _readService;
    private readonly ILogger<GetVideoBySlugQueryHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public GetVideoBySlugQueryHandler(
        ILibraryCatalogReadService readService,
        ILogger<GetVideoBySlugQueryHandler> logger)
    {
        _readService = readService;
        _logger = logger;
    }

    /// <summary>Returns the video detail DTO for the requested slug.</summary>
    /// <param name="request">The query containing the slug, location code, and current user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Video detail DTO.</returns>
    /// <exception cref="NotFoundException">Thrown when the slug does not match a published video.</exception>
    public async Task<LibraryVideoDetailResponse> Handle(
        GetVideoBySlugQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching library video by slug {Slug} for location {LocationCode}",
            request.Slug, request.LocationCode);

        var result = await _readService.GetVideoBySlugAsync(
            request.Slug, request.LocationCode, request.CurrentUserId, cancellationToken);

        if (result is null)
            throw new NotFoundException("LibraryVideo", request.Slug);

        _logger.LogInformation("Library video {Slug} returned successfully", request.Slug);
        return result;
    }
}
