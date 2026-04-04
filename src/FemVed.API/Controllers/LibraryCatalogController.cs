using System.Security.Claims;
using FemVed.Application.Library.DTOs;
using FemVed.Application.Library.Queries.GetFilterTypes;
using FemVed.Application.Library.Queries.GetLibraryCategoryBySlug;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Application.Library.Queries.GetVideoBySlug;
using FemVed.Application.Library.Queries.GetVideoStreamUrl;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Handles all public Wellness Library catalog operations: browsing the catalog tree,
/// viewing categories, video detail pages, and filter tabs.
/// Base route: /api/v1/library
/// </summary>
[ApiController]
[Route("api/v1/library")]
public sealed class LibraryCatalogController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public LibraryCatalogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Public read endpoints ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the full Wellness Library catalog tree (domain → filters → featured → categories → videos).
    /// Prices are resolved for the caller's location (JWT claim → query string → Accept-Language → GB).
    /// Response is cached for 10 minutes.
    /// </summary>
    /// <param name="countryCode">Optional country code override for price formatting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the full tree.</returns>
    [HttpGet("tree")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LibraryTreeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetLibraryTreeQuery(DetectLocationCode(countryCode)),
            cancellationToken);

        Response.Headers.Append("Cache-Control", "public, max-age=600, stale-while-revalidate=120");
        return Ok(result);
    }

    /// <summary>
    /// Returns a single library category with its published videos.
    /// Prices are resolved for the caller's location.
    /// </summary>
    /// <param name="slug">Category URL slug, e.g. "hormonal-health-support".</param>
    /// <param name="countryCode">Optional country code override for price formatting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the category, or 404 if not found.</returns>
    [HttpGet("categories/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LibraryCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(
        string slug,
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetLibraryCategoryBySlugQuery(slug, DetectLocationCode(countryCode)),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single published video's full detail page data by URL slug.
    /// Trailer URL is always included. Stream URLs are NEVER included here.
    /// If the caller is authenticated, the <c>isPurchased</c> flag is set accordingly.
    /// </summary>
    /// <param name="slug">Video URL slug, e.g. "cycle-reset-method".</param>
    /// <param name="countryCode">Optional country code override for price formatting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the video detail, or 404 if not found or not published.</returns>
    [HttpGet("videos/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LibraryVideoDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVideo(
        string slug,
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetVideoBySlugQuery(slug, DetectLocationCode(countryCode), GetCurrentUserIdOrNull()),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns all active filter tabs for the library catalog page.
    /// Includes a synthetic "All Programs" entry at position 0.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of filter types.</returns>
    [HttpGet("filters")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<LibraryFilterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilters(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFilterTypesQuery(), cancellationToken);
        Response.Headers.Append("Cache-Control", "public, max-age=600, stale-while-revalidate=120");
        return Ok(result);
    }

    // ── Authenticated endpoints ────────────────────────────────────────────────

    /// <summary>
    /// Returns stream URL(s) and watch progress for a purchased library video.
    /// Requires authentication and an active purchase.
    /// For Masterclass: returns the single stream URL.
    /// For Series: returns all episode stream URLs with per-episode watch progress.
    /// </summary>
    /// <param name="slug">Video URL slug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with stream data, 403 if not purchased, 404 if not found.</returns>
    [HttpGet("videos/{slug}/stream")]
    [Authorize]
    [ProducesResponseType(typeof(LibraryStreamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetVideoStream(
        string slug,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(
            new GetVideoStreamUrlQuery(slug, userId),
            cancellationToken);
        return Ok(result);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Detects the caller's ISO country code for price formatting.
    /// Resolution order: (1) explicit query string, (2) JWT country_iso_code claim,
    /// (3) Accept-Language header, (4) "GB".
    /// </summary>
    /// <param name="queryCountryCode">Value of the country_code query string parameter, if provided.</param>
    private string DetectLocationCode(string? queryCountryCode = null)
    {
        // 1. Explicit query string override — e.g. ?countryCode=IN
        if (!string.IsNullOrWhiteSpace(queryCountryCode))
            return queryCountryCode.ToUpperInvariant();

        // 2. Authenticated user's stored country code
        var claim = User.FindFirst("country_iso_code")?.Value;
        if (!string.IsNullOrWhiteSpace(claim))
            return claim;

        // 3. Accept-Language header — e.g. "en-IN,en;q=0.9" → "IN"
        var acceptLang = Request.Headers["Accept-Language"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(acceptLang))
        {
            var primary = acceptLang.Split(',')[0].Trim();
            var parts = primary.Split('-');
            if (parts.Length == 2 && parts[1].Length == 2)
                return parts[1].ToUpperInvariant();
        }

        // 4. Default
        return "GB";
    }

    /// <summary>Returns the authenticated user's ID from JWT claims (NameIdentifier or sub).</summary>
    /// <exception cref="InvalidOperationException">Thrown when the user ID claim is missing.</exception>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value
                 ?? throw new InvalidOperationException("User ID claim is missing from the JWT.");
        return Guid.Parse(value);
    }

    /// <summary>
    /// Returns the authenticated user's ID from JWT claims, or null for anonymous callers.
    /// Used by public endpoints that optionally use authentication context (e.g. isPurchased flag).
    /// </summary>
    private Guid? GetCurrentUserIdOrNull()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var id) ? id : null;
    }
}
