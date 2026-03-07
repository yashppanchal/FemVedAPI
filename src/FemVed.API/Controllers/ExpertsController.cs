using System.Security.Claims;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Enrollments.Commands.EndEnrollment;
using FemVed.Application.Enrollments.Commands.PauseEnrollment;
using FemVed.Application.Enrollments.Commands.ResumeEnrollment;
using FemVed.Application.Enrollments.Commands.StartEnrollment;
using FemVed.Application.Experts.Commands.SendProgressUpdate;
using FemVed.Application.Experts.Commands.UpdateMyExpertProfile;
using FemVed.Application.Experts.DTOs;
using FemVed.Application.Experts.Queries.GetEnrollmentComments;
using FemVed.Application.Experts.Queries.GetMyEarnings;
using FemVed.Application.Experts.Queries.GetMyEnrollments;
using FemVed.Application.Experts.Queries.GetMyExpertProfile;
using FemVed.Application.Experts.Queries.GetMyExpertPrograms;
using FemVed.Application.Experts.Queries.GetMyPayoutHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Handles expert dashboard operations: profile, program overview, enrollments,
/// session lifecycle actions (start/pause/resume/end), and progress comments.
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

    // ── Profile & Programs ────────────────────────────────────────────────────

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

    // ── Self-Profile Update ───────────────────────────────────────────────────

    /// <summary>
    /// Updates the authenticated expert's own profile fields.
    /// All body fields are optional — only provided (non-null) fields are updated.
    /// CommissionRate and IsActive can only be changed by an Admin via PUT /admin/experts/{id}.
    /// </summary>
    /// <param name="request">Profile fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the updated expert profile; 404 if no expert profile is linked to the account.</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ExpertProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyExpertProfile(
        [FromBody] UpdateMyExpertProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(
            new UpdateMyExpertProfileCommand(
                userId,
                request.DisplayName,
                request.Title,
                request.Bio,
                request.GridDescription,
                request.DetailedDescription,
                request.ProfileImageUrl,
                request.GridImageUrl,
                request.Specialisations,
                request.YearsExperience,
                request.Credentials,
                request.LocationCountry),
            cancellationToken);
        return Ok(result);
    }

    // ── Earnings & Payouts ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the authenticated expert's full earnings and payout balance sheet.
    /// Shows gross revenue from all their programs, their commission share,
    /// platform commission, total already paid out, and outstanding balance — all per currency.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the earnings balance sheet; 404 if no expert profile is linked to the account.</returns>
    [HttpGet("me/earnings")]
    [ProducesResponseType(typeof(ExpertPayoutBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyEarnings(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyEarningsQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns all payout records received by the authenticated expert, newest first.
    /// Each record shows the amount, currency, payment reference, and which admin made the payment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with list of payout records (may be empty); 404 if no expert profile is linked.</returns>
    [HttpGet("me/payouts")]
    [ProducesResponseType(typeof(List<ExpertPayoutRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPayoutHistory(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyPayoutHistoryQuery(userId), cancellationToken);
        return Ok(result);
    }

    // ── Enrollments ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all enrollment records across all of the expert's programs, newest first.
    /// Includes session lifecycle fields: pausedAt, endedBy, endedByRole.
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

    // ── Session Lifecycle ─────────────────────────────────────────────────────

    /// <summary>
    /// Starts an enrollment — transitions it from NOT_STARTED to ACTIVE.
    /// The enrollment must belong to the authenticated expert's own program.
    /// Emails the enrolled user a <c>session_started</c> notification.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record to start.</param>
    /// <param name="request">Optional note to log against this action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("me/enrollments/{accessId:guid}/start")]
    [ProducesResponseType(typeof(SessionActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> StartEnrollment(
        Guid accessId,
        [FromBody] SessionActionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(
            new StartEnrollmentCommand(accessId, userId, IsAdmin: false, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "started"));
    }

    /// <summary>
    /// Pauses an enrollment — transitions it from ACTIVE to PAUSED.
    /// The enrollment must belong to the authenticated expert's own program.
    /// Emails the enrolled user a <c>session_paused</c> notification.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record to pause.</param>
    /// <param name="request">Optional note to log against this action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("me/enrollments/{accessId:guid}/pause")]
    [ProducesResponseType(typeof(SessionActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PauseEnrollment(
        Guid accessId,
        [FromBody] SessionActionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(
            new PauseEnrollmentCommand(accessId, userId, IsAdmin: false, IsUser: false, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "paused"));
    }

    /// <summary>
    /// Resumes a paused enrollment — transitions it from PAUSED back to ACTIVE.
    /// The enrollment must belong to the authenticated expert's own program.
    /// Emails the enrolled user a <c>session_resumed</c> notification.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record to resume.</param>
    /// <param name="request">Optional note to log against this action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("me/enrollments/{accessId:guid}/resume")]
    [ProducesResponseType(typeof(SessionActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResumeEnrollment(
        Guid accessId,
        [FromBody] SessionActionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(
            new ResumeEnrollmentCommand(accessId, userId, IsAdmin: false, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "resumed"));
    }

    /// <summary>
    /// Ends an enrollment — transitions it from ACTIVE or PAUSED to COMPLETED.
    /// The enrollment must belong to the authenticated expert's own program.
    /// Emails the enrolled user a <c>session_ended</c> notification.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record to end.</param>
    /// <param name="request">Optional note to log against this action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("me/enrollments/{accessId:guid}/end")]
    [ProducesResponseType(typeof(SessionActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EndEnrollment(
        Guid accessId,
        [FromBody] SessionActionRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(
            new EndEnrollmentCommand(accessId, userId, IsAdmin: false, IsUser: false, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "ended"));
    }

    // ── Progress Comments ─────────────────────────────────────────────────────

    /// <summary>
    /// Sends a progress comment to a specific enrolled user.
    /// Always dispatches an email via SendGrid (<c>expert_progress_update</c> template).
    /// The <paramref name="accessId"/> must belong to the expert's own programs.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record (from GET /me/enrollments).</param>
    /// <param name="request">The comment text (10–2000 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with confirmation payload.</returns>
    [HttpPost("me/enrollments/{accessId:guid}/comments")]
    [ProducesResponseType(typeof(CommentSentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendComment(
        Guid accessId,
        [FromBody] SendCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(
            new SendProgressUpdateCommand(userId, accessId, request.UpdateNote, IsAdmin: false),
            cancellationToken);
        return Ok(new CommentSentResponse(accessId, true));
    }

    /// <summary>
    /// Returns all comments sent for a specific enrollment, oldest first.
    /// The <paramref name="accessId"/> must belong to the expert's own programs.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with list of comments (may be empty).</returns>
    [HttpGet("me/enrollments/{accessId:guid}/comments")]
    [ProducesResponseType(typeof(List<EnrollmentCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComments(
        Guid accessId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(
            new GetEnrollmentCommentsQuery(accessId, userId, IsAdmin: false),
            cancellationToken);
        return Ok(result);
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
    /// Returns 404 if the account has no expert profile.
    /// </summary>
    private async Task<Guid> GetCurrentExpertIdAsync(CancellationToken cancellationToken)
    {
        var userId  = GetCurrentUserId();
        var profile = await _mediator.Send(new GetMyExpertProfileQuery(userId), cancellationToken);
        return profile.ExpertId;
    }
}

// ── Request / Response body records ───────────────────────────────────────────
// Note: SessionActionRequest, SessionActionResponse, SendCommentRequest, CommentSentResponse
//       are defined in src/FemVed.API/Models/SessionModels.cs (shared with UsersController).

/// <summary>HTTP request body for PUT /api/v1/experts/me. All fields are optional.</summary>
/// <param name="DisplayName">New public display name. Null = no change.</param>
/// <param name="Title">New professional title. Null = no change.</param>
/// <param name="Bio">New full biography text. Null = no change.</param>
/// <param name="GridDescription">New short bio for grid cards (max 500 chars). Null = no change.</param>
/// <param name="DetailedDescription">New detailed bio for program detail page. Null = no change.</param>
/// <param name="ProfileImageUrl">New profile photo URL. Null = no change.</param>
/// <param name="GridImageUrl">New grid card image URL. Null = no change.</param>
/// <param name="Specialisations">New list of specialisation areas. Null = no change.</param>
/// <param name="YearsExperience">New years of experience. Null = no change.</param>
/// <param name="Credentials">New list of credentials/certifications. Null = no change.</param>
/// <param name="LocationCountry">New country. Null = no change.</param>
public record UpdateMyExpertProfileRequest(
    string? DisplayName,
    string? Title,
    string? Bio,
    string? GridDescription,
    string? DetailedDescription,
    string? ProfileImageUrl,
    string? GridImageUrl,
    List<string>? Specialisations,
    short? YearsExperience,
    List<string>? Credentials,
    string? LocationCountry);
