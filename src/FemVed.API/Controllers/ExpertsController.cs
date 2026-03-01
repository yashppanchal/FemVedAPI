using System.Security.Claims;
using FemVed.Application.Experts.Commands.SendProgressUpdate;
using FemVed.Application.Experts.DTOs;
using FemVed.Application.Experts.Queries.GetMyEnrollments;
using FemVed.Application.Experts.Queries.GetMyExpertProfile;
using FemVed.Application.Experts.Queries.GetMyExpertPrograms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Handles expert dashboard operations: profile, program overview, enrollments, and progress updates.
/// All endpoints require the ExpertOrAdmin policy.
/// Base route: /api/v1/experts
/// </summary>
[ApiController]
[Route("api/v1/experts")]
[Authorize(Policy = "ExpertOrAdmin")]
public sealed class ExpertsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public ExpertsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the expert profile linked to the authenticated user account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the expert profile; 404 if no expert profile is linked to the account.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ExpertProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyExpertProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyExpertProfileQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a summary of all programs belonging to the authenticated expert,
    /// including per-program active and total enrollment counts. All statuses are included.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of program summaries (may be empty).</returns>
    [HttpGet("me/programs")]
    [ProducesResponseType(typeof(List<ExpertProgramSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyExpertPrograms(CancellationToken cancellationToken)
    {
        var expertId = await GetCurrentExpertIdAsync(cancellationToken);
        var result   = await _mediator.Send(new GetMyExpertProgramsQuery(expertId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns all enrollment records across all of the expert's programs, newest first.
    /// Includes enrolled user details so the expert can identify who to contact.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of enrollments (may be empty).</returns>
    [HttpGet("me/enrollments")]
    [ProducesResponseType(typeof(List<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrollments(CancellationToken cancellationToken)
    {
        var expertId = await GetCurrentExpertIdAsync(cancellationToken);
        var result   = await _mediator.Send(new GetMyEnrollmentsQuery(expertId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Sends a progress update note to a specific enrolled user.
    /// The <paramref name="accessId"/> must belong to the expert's own programs — attempting to
    /// update another expert's enrollment returns 403 Forbidden.
    /// Optionally also sends the note as an email via SendGrid (<c>expert_progress_update</c> template).
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record (from GET /me/enrollments).</param>
    /// <param name="request">The update note and optional email flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("me/enrollments/{accessId:guid}/progress-update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendProgressUpdate(
        Guid accessId,
        [FromBody] SendProgressUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var expertId = await GetCurrentExpertIdAsync(cancellationToken);
        await _mediator.Send(
            new SendProgressUpdateCommand(expertId, accessId, request.UpdateNote, request.SendEmail),
            cancellationToken);
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Returns the authenticated user's ID from JWT claims (NameIdentifier or sub).</summary>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User ID claim is missing from the JWT.");
        return Guid.Parse(value);
    }

    /// <summary>
    /// Resolves the expert ID for the authenticated user by looking up the expert profile.
    /// Throws <see cref="Application.Experts.Queries.GetMyExpertProfile.GetMyExpertProfileQuery"/>
    /// internally — returns 404 if the account has no expert profile.
    /// </summary>
    private async Task<Guid> GetCurrentExpertIdAsync(CancellationToken cancellationToken)
    {
        var userId  = GetCurrentUserId();
        var profile = await _mediator.Send(new GetMyExpertProfileQuery(userId), cancellationToken);
        return profile.ExpertId;
    }
}

// ── Request body records ──────────────────────────────────────────────────────

/// <summary>HTTP request body for POST /api/v1/experts/me/enrollments/{accessId}/progress-update.</summary>
/// <param name="UpdateNote">The progress note content (10–2000 characters).</param>
/// <param name="SendEmail">When true, also sends the note as an email to the enrolled user.</param>
public record SendProgressUpdateRequest(string UpdateNote, bool SendEmail);
