using System.Security.Claims;
using FemVed.Application.Guided.Commands.AddDuration;
using FemVed.Application.Guided.Commands.AddDurationPrice;
using FemVed.Application.Guided.Commands.ArchiveProgram;
using FemVed.Application.Guided.Commands.CreateCategory;
using FemVed.Application.Guided.Commands.DeleteDuration;
using FemVed.Application.Guided.Commands.DeleteDurationPrice;
using FemVed.Application.Guided.Commands.UpdateDuration;
using FemVed.Application.Guided.Commands.UpdateDurationPrice;
using FemVed.Application.Guided.Queries.GetProgramDurations;
using FemVed.Application.Guided.Commands.CreateDomain;
using FemVed.Application.Guided.Commands.CreateProgram;
using FemVed.Application.Guided.Commands.DeleteCategory;
using FemVed.Application.Guided.Commands.DeleteDomain;
using FemVed.Application.Guided.Commands.DeleteProgram;
using FemVed.Application.Guided.Commands.PublishProgram;
using FemVed.Application.Guided.Commands.SubmitProgramForReview;
using FemVed.Application.Guided.Commands.UpdateCategory;
using FemVed.Application.Guided.Commands.UpdateDomain;
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
    public async Task<IActionResult> GetTree(
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetGuidedTreeQuery(DetectLocationCode(countryCode)),
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
    public async Task<IActionResult> GetCategory(
        string slug,
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetCategoryBySlugQuery(slug, DetectLocationCode(countryCode)),
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
    public async Task<IActionResult> GetProgram(
        string slug,
        [FromQuery] string? countryCode,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetProgramBySlugQuery(slug, DetectLocationCode(countryCode)),
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
            new CreateDomainCommand(request.Name, request.Slug, request.SortOrder, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return CreatedAtAction(nameof(GetTree), new { }, id);
    }

    /// <summary>
    /// Updates an existing guided domain's name, slug, or sort order. Admin only.
    /// All fields are optional — only non-null values are applied.
    /// </summary>
    /// <param name="id">Domain ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with update confirmation payload.</returns>
    [HttpPut("domains/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(UpdateResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDomain(
        Guid id,
        [FromBody] UpdateDomainRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateDomainCommand(id, request.Name, request.Slug, request.SortOrder,
                GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return Ok(new UpdateResultResponse(id, true));
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
    /// <returns>200 OK with update confirmation payload.</returns>
    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(UpdateResultResponse), StatusCodes.Status200OK)]
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
        return Ok(new UpdateResultResponse(id, true));
    }

    /// <summary>
    /// Soft-deletes a guided domain (sets is_deleted = true). Admin only.
    /// </summary>
    /// <param name="id">Domain ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with delete confirmation payload.</returns>
    [HttpDelete("domains/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DeleteResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDomain(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteDomainCommand(id, GetCurrentUserId(), GetIpAddress()), cancellationToken);
        return Ok(new DeleteResultResponse(id, true));
    }

    /// <summary>
    /// Soft-deletes a guided category (sets is_deleted = true). Admin only.
    /// </summary>
    /// <param name="id">Category ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with delete confirmation payload.</returns>
    [HttpDelete("categories/{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DeleteResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCategoryCommand(id, GetCurrentUserId(), GetIpAddress()), cancellationToken);
        return Ok(new DeleteResultResponse(id, true));
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
                request.Tags,
                request.DetailSections),
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
    /// <returns>200 OK with update confirmation payload.</returns>
    [HttpPut("programs/{id:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(UpdateResultResponse), StatusCodes.Status200OK)]
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
                request.SortOrder,
                request.DetailSections),
            cancellationToken);
        return Ok(new UpdateResultResponse(id, true));
    }

    /// <summary>
    /// Submits a DRAFT program for Admin review (DRAFT → PENDING_REVIEW). Expert or Admin.
    /// </summary>
    /// <param name="id">Program ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with lifecycle confirmation payload.</returns>
    [HttpPost("programs/{id:guid}/submit")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(ProgramLifecycleResultResponse), StatusCodes.Status200OK)]
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
        return Ok(new ProgramLifecycleResultResponse(id, "PENDING_REVIEW", true));
    }

    /// <summary>
    /// Publishes a PENDING_REVIEW program (PENDING_REVIEW → PUBLISHED). Admin only.
    /// Evicts the guided tree cache immediately.
    /// </summary>
    /// <param name="id">Program ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with lifecycle confirmation payload.</returns>
    [HttpPost("programs/{id:guid}/publish")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ProgramLifecycleResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new PublishProgramCommand(id), cancellationToken);
        return Ok(new ProgramLifecycleResultResponse(id, "PUBLISHED", true));
    }

    /// <summary>
    /// Archives a PUBLISHED program (PUBLISHED → ARCHIVED). Admin only.
    /// Evicts the guided tree cache immediately so the program disappears from public view.
    /// </summary>
    /// <param name="id">Program ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with lifecycle confirmation payload.</returns>
    [HttpPost("programs/{id:guid}/archive")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(ProgramLifecycleResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ArchiveProgramCommand(id), cancellationToken);
        return Ok(new ProgramLifecycleResultResponse(id, "ARCHIVED", true));
    }

    /// <summary>
    /// Soft-deletes a program (sets is_deleted = true). Expert or Admin.
    /// Experts may only delete their own programs.
    /// </summary>
    /// <param name="id">Program ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with delete confirmation payload.</returns>
    [HttpDelete("programs/{id:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(DeleteResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProgram(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteProgramCommand(id, GetCurrentUserId(), User.IsInRole("Admin"), GetIpAddress()),
            cancellationToken);
        return Ok(new DeleteResultResponse(id, true));
    }

    // ── Duration management (ExpertOrAdmin) ──────────────────────────────────

    /// <summary>
    /// Lists all durations for a program with all location prices (all countries, active and inactive).
    /// Management view — unlike <c>GET /guided/tree</c> which filters to one location. Expert or Admin.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of durations, or 403 if not the owner.</returns>
    [HttpGet("programs/{programId:guid}/durations")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(List<DurationManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDurations(Guid programId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetProgramDurationsQuery(programId, GetCurrentUserId(), User.IsInRole("Admin")),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single duration with all location prices. Expert or Admin.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="durationId">Duration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the duration, or 404 if not found.</returns>
    [HttpGet("programs/{programId:guid}/durations/{durationId:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(DurationManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDuration(
        Guid programId, Guid durationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetProgramDurationsQuery(programId, GetCurrentUserId(), User.IsInRole("Admin"), durationId),
            cancellationToken);
        return Ok(result.Single());
    }

    /// <summary>
    /// Adds a new duration (with prices) to an existing program. Expert or Admin.
    /// Experts may only add durations to DRAFT or PENDING_REVIEW programs.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="request">Duration details including at least one location price.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new duration ID.</returns>
    [HttpPost("programs/{programId:guid}/durations")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddDuration(
        Guid programId,
        [FromBody] AddDurationRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new AddDurationCommand(
                programId, GetCurrentUserId(), User.IsInRole("Admin"),
                request.Label, request.Weeks, request.SortOrder, request.Prices),
            cancellationToken);
        return CreatedAtAction(nameof(GetDuration), new { programId, durationId = id }, id);
    }

    /// <summary>
    /// Updates a duration's label, week count, or sort order. Expert or Admin.
    /// All body fields are optional — only non-null values are applied.
    /// Experts may only modify DRAFT or PENDING_REVIEW programs.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="durationId">Duration ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with update confirmation payload.</returns>
    [HttpPut("programs/{programId:guid}/durations/{durationId:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(UpdateResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDuration(
        Guid programId,
        Guid durationId,
        [FromBody] UpdateDurationRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateDurationCommand(
                durationId, programId, GetCurrentUserId(), User.IsInRole("Admin"),
                request.Label, request.Weeks, request.SortOrder),
            cancellationToken);
        return Ok(new UpdateResultResponse(durationId, true));
    }

    /// <summary>
    /// Deactivates a duration (sets is_active = false). Data is preserved. Expert or Admin.
    /// Experts may only deactivate durations on DRAFT or PENDING_REVIEW programs.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="durationId">Duration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with deactivation confirmation payload.</returns>
    [HttpDelete("programs/{programId:guid}/durations/{durationId:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(DeleteResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveDuration(
        Guid programId, Guid durationId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteDurationCommand(durationId, programId, GetCurrentUserId(), User.IsInRole("Admin")),
            cancellationToken);
        return Ok(new DeleteResultResponse(durationId, true));
    }

    // ── Price management (ExpertOrAdmin) ──────────────────────────────────────

    /// <summary>
    /// Lists all prices for a duration (all locations, active and inactive). Expert or Admin.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="durationId">Duration ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the flat list of prices.</returns>
    [HttpGet("programs/{programId:guid}/durations/{durationId:guid}/prices")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(List<DurationPriceManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrices(
        Guid programId, Guid durationId, CancellationToken cancellationToken)
    {
        var durations = await _mediator.Send(
            new GetProgramDurationsQuery(programId, GetCurrentUserId(), User.IsInRole("Admin"), durationId),
            cancellationToken);
        return Ok(durations.Single().Prices);
    }

    /// <summary>
    /// Returns a single price record. Expert or Admin.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="durationId">Duration ID.</param>
    /// <param name="priceId">Price ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the price, or 404 if not found.</returns>
    [HttpGet("programs/{programId:guid}/durations/{durationId:guid}/prices/{priceId:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(DurationPriceManagementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrice(
        Guid programId, Guid durationId, Guid priceId, CancellationToken cancellationToken)
    {
        var durations = await _mediator.Send(
            new GetProgramDurationsQuery(programId, GetCurrentUserId(), User.IsInRole("Admin"), durationId),
            cancellationToken);
        var price = durations.Single().Prices.FirstOrDefault(p => p.PriceId == priceId);
        if (price is null) return NotFound();
        return Ok(price);
    }

    /// <summary>
    /// Adds a new location price to a duration. Expert or Admin.
    /// One active price per location per duration is enforced — deactivate the existing one first if needed.
    /// Experts may only modify DRAFT or PENDING_REVIEW programs.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="durationId">Duration ID.</param>
    /// <param name="request">Price details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new price ID.</returns>
    [HttpPost("programs/{programId:guid}/durations/{durationId:guid}/prices")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddPrice(
        Guid programId,
        Guid durationId,
        [FromBody] AddDurationPriceRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new AddDurationPriceCommand(
                durationId, programId, GetCurrentUserId(), User.IsInRole("Admin"),
                request.LocationCode, request.Amount, request.CurrencyCode, request.CurrencySymbol),
            cancellationToken);
        return CreatedAtAction(nameof(GetPrice), new { programId, durationId, priceId = id }, id);
    }

    /// <summary>
    /// Updates a price's amount, currency code, or currency symbol. Expert or Admin.
    /// All body fields are optional — only non-null values are applied.
    /// Experts may only modify DRAFT or PENDING_REVIEW programs.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="durationId">Duration ID.</param>
    /// <param name="priceId">Price ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with update confirmation payload.</returns>
    [HttpPut("programs/{programId:guid}/durations/{durationId:guid}/prices/{priceId:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(UpdateResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdatePrice(
        Guid programId,
        Guid durationId,
        Guid priceId,
        [FromBody] UpdateDurationPriceRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateDurationPriceCommand(
                priceId, durationId, programId, GetCurrentUserId(), User.IsInRole("Admin"),
                request.Amount, request.CurrencyCode, request.CurrencySymbol),
            cancellationToken);
        return Ok(new UpdateResultResponse(priceId, true));
    }

    /// <summary>
    /// Deactivates a price (sets is_active = false). Data is preserved. Expert or Admin.
    /// Experts may only deactivate prices on DRAFT or PENDING_REVIEW programs.
    /// </summary>
    /// <param name="programId">Program ID.</param>
    /// <param name="durationId">Duration ID.</param>
    /// <param name="priceId">Price ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with deactivation confirmation payload.</returns>
    [HttpDelete("programs/{programId:guid}/durations/{durationId:guid}/prices/{priceId:guid}")]
    [Authorize(Policy = "ExpertOrAdmin")]
    [ProducesResponseType(typeof(DeleteResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemovePrice(
        Guid programId,
        Guid durationId,
        Guid priceId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteDurationPriceCommand(priceId, durationId, programId, GetCurrentUserId(), User.IsInRole("Admin")),
            cancellationToken);
        return Ok(new DeleteResultResponse(priceId, true));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects the caller's ISO country code for price formatting.
    /// Resolution order: (1) explicit <paramref name="queryCountryCode"/> query string, (2) JWT <c>country_iso_code</c> claim, (3) Accept-Language header, (4) "GB".
    /// </summary>
    /// <param name="queryCountryCode">Value of the <c>country_code</c> query string parameter, if provided.</param>
    private string DetectLocationCode(string? queryCountryCode = null)
    {
        // 1. Explicit query string override — e.g. ?country_code=IN
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
            var primary = acceptLang.Split(',')[0].Trim(); // e.g. "en-IN"
            var parts = primary.Split('-');
            if (parts.Length == 2 && parts[1].Length == 2)
                return parts[1].ToUpperInvariant();
        }

        // 4. Default
        return "GB";
    }

    /// <summary>Returns the authenticated user's ID from JWT claims (NameIdentifier or sub).</summary>
    /// <exception cref="InvalidOperationException">Thrown if the user ID claim is missing (should never happen on [Authorize] endpoints).</exception>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User ID claim is missing from the JWT.");
        return Guid.Parse(value);
    }

    /// <summary>Returns the client's remote IP address, or null if unavailable.</summary>
    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}

// ── HTTP request body records ─────────────────────────────────────────────────

/// <summary>HTTP request body for POST /api/v1/guided/domains.</summary>
/// <param name="Name">Domain display name.</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="SortOrder">Display order (default 0).</param>
public record CreateDomainRequest(string Name, string Slug, int SortOrder = 0);

/// <summary>HTTP request body for PUT /api/v1/guided/domains/{id}. All fields optional.</summary>
/// <param name="Name">New display name (optional).</param>
/// <param name="Slug">New URL slug — must be unique (optional).</param>
/// <param name="SortOrder">New display order (optional).</param>
public record UpdateDomainRequest(string? Name, string? Slug, int? SortOrder);

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
    List<string> Tags,
    List<DetailSectionInput> DetailSections);

/// <summary>HTTP request body for PUT /api/v1/guided/programs/{id}. All fields optional.</summary>
public record UpdateProgramRequest(
    string? Name,
    string? GridDescription,
    string? GridImageUrl,
    string? Overview,
    int? SortOrder,
    List<DetailSectionInput>? DetailSections);

/// <summary>Standard delete success payload returned by Guided DELETE endpoints.</summary>
/// <param name="Id">ID of the deleted resource.</param>
/// <param name="IsDeleted">Always true when deletion succeeds.</param>
public record DeleteResultResponse(Guid Id, bool IsDeleted);

/// <summary>Standard update success payload returned by Guided PUT endpoints.</summary>
/// <param name="Id">ID of the updated resource.</param>
/// <param name="IsUpdated">Always true when update succeeds.</param>
public record UpdateResultResponse(Guid Id, bool IsUpdated);

/// <summary>Lifecycle transition payload returned by Guided program state POST endpoints.</summary>
/// <param name="Id">ID of the program.</param>
/// <param name="Status">Final program status after the transition.</param>
/// <param name="IsUpdated">Always true when transition succeeds.</param>
public record ProgramLifecycleResultResponse(Guid Id, string Status, bool IsUpdated);

/// <summary>HTTP request body for POST /api/v1/guided/programs/{programId}/durations.</summary>
/// <param name="Label">Human-readable label, e.g. "6 weeks".</param>
/// <param name="Weeks">Duration length in weeks.</param>
/// <param name="SortOrder">Display order (default 0).</param>
/// <param name="Prices">One or more location-specific prices for this duration.</param>
public record AddDurationRequest(string Label, short Weeks, int SortOrder, List<DurationPriceInput> Prices);

/// <summary>HTTP request body for PUT /api/v1/guided/programs/{programId}/durations/{durationId}. All fields optional.</summary>
/// <param name="Label">New human-readable label (optional).</param>
/// <param name="Weeks">New duration in weeks (optional).</param>
/// <param name="SortOrder">New display order (optional).</param>
public record UpdateDurationRequest(string? Label, short? Weeks, int? SortOrder);

/// <summary>HTTP request body for POST /api/v1/guided/programs/{programId}/durations/{durationId}/prices.</summary>
/// <param name="LocationCode">ISO country code, e.g. "GB", "IN", "US".</param>
/// <param name="Amount">Price amount, e.g. 320.00.</param>
/// <param name="CurrencyCode">ISO 4217 code, e.g. "GBP".</param>
/// <param name="CurrencySymbol">Display symbol, e.g. "£".</param>
public record AddDurationPriceRequest(string LocationCode, decimal Amount, string CurrencyCode, string CurrencySymbol);

/// <summary>HTTP request body for PUT /api/v1/guided/programs/{programId}/durations/{durationId}/prices/{priceId}. All fields optional.</summary>
/// <param name="Amount">New price amount (optional).</param>
/// <param name="CurrencyCode">New ISO 4217 code (optional).</param>
/// <param name="CurrencySymbol">New display symbol (optional).</param>
public record UpdateDurationPriceRequest(decimal? Amount, string? CurrencyCode, string? CurrencySymbol);
