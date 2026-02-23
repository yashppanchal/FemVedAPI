using System.Security.Claims;
using FemVed.Application.Guided.Commands.ArchiveProgram;
using FemVed.Application.Guided.Commands.CreateCategory;
using FemVed.Application.Guided.Commands.CreateDomain;
using FemVed.Application.Guided.Commands.CreateProgram;
using FemVed.Application.Guided.Commands.PublishProgram;
using FemVed.Application.Guided.Commands.SubmitProgramForReview;
using FemVed.Application.Guided.Commands.UpdateCategory;
using FemVed.Application.Guided.Commands.UpdateProgram;
using FemVed.Application.Guided.DTOs;
using FemVed.Application.Guided.Queries.GetCategoryBySlug;
using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Guided.Queries.GetProgramBySlug;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Handles all Guided Catalog operations: browsing the catalog tree, managing domains,
/// categories, and programs (CRUD + lifecycle transitions).
/// Base route: /api/v1/guided
/// </summary>
[ApiController]
[Route("api/v1/guided")]
public sealed class GuidedController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public GuidedController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Public read endpoints ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the full guided catalog tree (domains → categories → programs).
    /// Prices are resolved for the caller's location (JWT claim → Accept-Language → GB).
    /// Response is cached for 10 minutes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the full tree.</returns>
    [HttpGet("tree")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GuidedTreeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetGuidedTreeQuery(DetectLocationCode()),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single category page with its published programs.
    /// Prices are resolved for the caller's location.
    /// </summary>
    /// <param name="slug">Category URL slug, e.g. "hormonal-health-support".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the category, or 404 if not found.</returns>
    [HttpGet("categories/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GuidedCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(string slug, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCategoryBySlugQuery(slug, DetectLocationCode()),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single published program detail page by URL slug.
    /// Prices are resolved for the caller's location.
    /// </summary>
    /// <param name="slug">Program URL slug, e.g. "break-stress-hormone-health-triangle".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the program, or 404 if not found or not published.</returns>
    [HttpGet("programs/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProgramInCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgram(string slug, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetProgramBySlugQuery(slug, DetectLocationCode()),
            cancellationToken);
        return Ok(result);
    }

    // ── Admin-only domain + category management ───────────────────────────────

    /// <summary>
    /// Creates a new guided domain (e.g. "Guided 1:1 Care"). Admin only.
    /// </summary>
    /// <param name="request">Domain creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new domain ID.</returns>
    [HttpPost("domains")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateDomain(
        [FromBody] CreateDomainRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateDomainCommand(request.Name, request.Slug, request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetTree), new { }, id);
    }

    /// <summary>
    /// Creates a new category within a domain. Admin only.
    /// </summary>
    /// <param name="request">Category creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new category ID.</returns>
    [HttpPost("categories")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateCategoryCommand(
                request.DomainId,
                request.Name,
                request.Slug,
                request.CategoryType,
                request.HeroTitle,
                request.HeroSubtext,
                request.CtaLabel,
                request.CtaLink,
                request.PageHeader,
                request.ImageUrl,
                request.SortOrder,
                request.ParentId,
                request.WhatsIncluded,
                request.KeyAreas),
            cancellationToken);
        return CreatedAtAction(nameof(GetCategory), new { slug = request.Slug }, id);
    }

    /// <summary>
    /// Updates an existing category's content. Admin only.
    /// All fields are optional — only non-null values are applied.
    /// </summary>
    /// <param name="id">Category ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateCategoryCommand(
                id,
                request.Name,
                request.CategoryType,
                request.HeroTitle,
                request.HeroSubtext,
                request.CtaLabel,
                request.CtaLink,
                request.PageHeader,
                request.ImageUrl,
                request.SortOrder,
                request.WhatsIncluded,
                request.KeyAreas),
            cancellationToken);
        return NoContent();
    }

    // ── Expert + Admin program management ─────────────────────────────────────

    /// <summary>
    /// Creates a new program as DRAFT. Expert creates for their own profile. Expert or Admin.
    /// </summary>
    /// <param name="request">Program creation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new program ID.</returns>
    [HttpPost("programs")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProgram(
        [FromBody] CreateProgramRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var id = await _mediator.Send(
            new CreateProgramCommand(
                userId,
                request.CategoryId,
                request.Name,
                request.Slug,
                request.GridDescription,
                request.GridImageUrl,
                request.Overview,
                request.SortOrder,
                request.Durations,
                request.WhatYouGet,
                request.WhoIsThisFor,
                request.Tags),
            cancellationToken);
        return CreatedAtAction(nameof(GetProgram), new { slug = request.Slug }, id);
    }

    /// <summary>
    /// Updates an existing program. Experts may only update their own DRAFT or PENDING_REVIEW programs.
    /// Admins may update any program in any status. Expert or Admin.
    /// </summary>
    /// <param name="id">Program ID.</param>
    /// <param name="request">Fields to update (all optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("programs/{id:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateProgram(
        Guid id,
        [FromBody] UpdateProgramRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        await _mediator.Send(
            new UpdateProgramCommand(
                id,
                userId,
                isAdmin,
                request.Name,
                request.GridDescription,
                request.GridImageUrl,
                request.Overview,
                request.SortOrder),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Submits a DRAFT program for Admin review (DRAFT → PENDING_REVIEW). Expert or Admin.
    /// </summary>
    /// <param name="id">Program ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("programs/{id:guid}/submit")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        await _mediator.Send(
            new SubmitProgramForReviewCommand(id, userId, isAdmin),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Publishes a PENDING_REVIEW program (PENDING_REVIEW → PUBLISHED). Admin only.
    /// Evicts the guided tree cache immediately.
    /// </summary>
    /// <param name="id">Program ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("programs/{id:guid}/publish")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new PublishProgramCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Archives a PUBLISHED program (PUBLISHED → ARCHIVED). Admin only.
    /// Evicts the guided tree cache immediately so the program disappears from public view.
    /// </summary>
    /// <param name="id">Program ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("programs/{id:guid}/archive")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveProgramCommand(id), cancellationToken);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the caller's ISO country code for price formatting.
    /// Resolution order: (1) JWT <c>country_iso_code</c> claim, (2) Accept-Language header, (3) "GB".
    /// </summary>
    private string DetectLocationCode()
    {
        // 1. Authenticated user's stored country code
        var claim = User.FindFirst("country_iso_code")?.Value;
        if (!string.IsNullOrWhiteSpace(claim))
            return claim;

        // 2. Accept-Language header — e.g. "en-IN,en;q=0.9" → "IN"
        var acceptLang = Request.Headers["Accept-Language"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(acceptLang))
        {
            var primary = acceptLang.Split(',')[0].Trim(); // e.g. "en-IN"
            var parts = primary.Split('-');
            if (parts.Length == 2 && parts[1].Length == 2)
                return parts[1].ToUpperInvariant();
        }

        // 3. Default
        return "GB";
    }

    /// <summary>Returns the authenticated user's ID from the JWT NameIdentifier claim.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the claim is missing (should never happen on [Authorize] endpoints).</exception>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("NameIdentifier claim is missing from the JWT.");
        return Guid.Parse(value);
    }
}

// ── HTTP request body records ─────────────────────────────────────────────────

/// <summary>HTTP request body for POST /api/v1/guided/domains.</summary>
/// <param name="Name">Domain display name.</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="SortOrder">Display order (default 0).</param>
public record CreateDomainRequest(string Name, string Slug, int SortOrder = 0);

/// <summary>HTTP request body for POST /api/v1/guided/categories.</summary>
public record CreateCategoryRequest(
    Guid DomainId,
    string Name,
    string Slug,
    string CategoryType,
    string HeroTitle,
    string HeroSubtext,
    string? CtaLabel,
    string? CtaLink,
    string? PageHeader,
    string? ImageUrl,
    int SortOrder,
    Guid? ParentId,
    List<string> WhatsIncluded,
    List<string> KeyAreas);

/// <summary>HTTP request body for PUT /api/v1/guided/categories/{id}. All fields optional.</summary>
public record UpdateCategoryRequest(
    string? Name,
    string? CategoryType,
    string? HeroTitle,
    string? HeroSubtext,
    string? CtaLabel,
    string? CtaLink,
    string? PageHeader,
    string? ImageUrl,
    int? SortOrder,
    List<string>? WhatsIncluded,
    List<string>? KeyAreas);

/// <summary>HTTP request body for POST /api/v1/guided/programs.</summary>
public record CreateProgramRequest(
    Guid CategoryId,
    string Name,
    string Slug,
    string GridDescription,
    string? GridImageUrl,
    string Overview,
    int SortOrder,
    List<DurationInput> Durations,
    List<string> WhatYouGet,
    List<string> WhoIsThisFor,
    List<string> Tags);

/// <summary>HTTP request body for PUT /api/v1/guided/programs/{id}. All fields optional.</summary>
public record UpdateProgramRequest(
    string? Name,
    string? GridDescription,
    string? GridImageUrl,
    string? Overview,
    int? SortOrder);
