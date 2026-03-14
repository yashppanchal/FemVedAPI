using System.Security.Claims;
using FemVed.Application.Enrollments.Commands.EndEnrollment;
using FemVed.Application.Enrollments.Commands.PauseEnrollment;
using FemVed.Application.Enrollments.Commands.RequestStartDate;
using FemVed.Application.Payments.DTOs;
using FemVed.Application.Payments.Queries.GetMyOrders;
using FemVed.Application.Payments.Queries.GetMyRefunds;
using FemVed.Application.Users.Commands.RequestGdprDeletion;
using FemVed.Application.Users.Commands.UpdateMyProfile;
using FemVed.Application.Users.DTOs;
using FemVed.Application.Users.Queries.GetMyProfile;
using FemVed.Application.Users.Queries.GetMyProgramAccess;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Handles authenticated-user operations: profile management, program access, and GDPR.
/// Base route: /api/v1/users
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the authenticated user's full profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the user profile.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyProfileQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the authenticated user's editable profile fields.
    /// Email is not editable. Country code and mobile number must be supplied together or both omitted.
    /// </summary>
    /// <param name="request">Updated profile fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the updated profile.</returns>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(
            new UpdateMyProfileCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.CountryCode,
                request.MobileNumber,
                request.WhatsAppOptIn),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns all program access records belonging to the authenticated user, newest first.
    /// An empty list is returned when the user has not purchased any programs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of program access records.</returns>
    [HttpGet("me/program-access")]
    [ProducesResponseType(typeof(List<ProgramAccessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProgramAccess(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyProgramAccessQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns all orders belonging to the authenticated user, newest first.
    /// An empty list is returned when the user has no orders.
    /// This is a convenience alias for <c>GET /api/v1/orders/my</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of orders.</returns>
    [HttpGet("me/orders")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyOrdersQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns all refunds issued against the authenticated user's orders, newest first.
    /// An empty list is returned when the user has no refunds.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of refund records.</returns>
    [HttpGet("me/refunds")]
    [ProducesResponseType(typeof(List<RefundDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRefunds(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyRefundsQuery(userId), cancellationToken);
        return Ok(result);
    }

    // ── Session Lifecycle (User) ──────────────────────────────────────────────

    /// <summary>
    /// Submits a preferred start date request for a NotStarted enrollment.
    /// The expert or admin must then approve or decline the request.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record.</param>
    /// <param name="request">The preferred start date as an ISO-8601 date string (e.g. "2026-04-01").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("me/enrollments/{accessId:guid}/request-start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestStartDate(
        Guid accessId,
        [FromBody] RequestStartDateRequest request,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(request.RequestedStartDate, out var date))
            return BadRequest("Invalid requestedStartDate format. Use ISO-8601 (e.g. \"2026-04-01\").");

        var userId = GetCurrentUserId();
        await _mediator.Send(new RequestStartDateCommand(accessId, userId, date), cancellationToken);
        return Ok(new { AccessId = accessId, Status = "Pending" });
    }

    /// <summary>
    /// Pauses one of the user's own active enrollments — transitions it from ACTIVE to PAUSED.
    /// Emails the user a <c>session_paused</c> notification.
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
            new PauseEnrollmentCommand(accessId, userId, IsAdmin: false, IsUser: true, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "paused"));
    }

    /// <summary>
    /// Ends one of the user's own enrollments — transitions it from ACTIVE or PAUSED to COMPLETED.
    /// Emails the user a <c>session_ended</c> notification.
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
            new EndEnrollmentCommand(accessId, userId, IsAdmin: false, IsUser: true, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "ended"));
    }

    // ── GDPR ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Submits a GDPR right-to-erasure request (for GB/EU users).
    /// The request is stored with status Pending and processed manually by an Admin.
    /// Submitting again while a Pending request already exists returns 202 without creating a duplicate.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>202 Accepted.</returns>
    [HttpPost("me/gdpr-deletion-request")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestGdprDeletion(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new RequestGdprDeletionCommand(userId), cancellationToken);
        return Accepted(new { message = "Your data erasure request has been received and will be processed within 30 days." });
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Returns the authenticated user's ID from JWT claims (NameIdentifier or sub).</summary>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User ID claim is missing from the JWT.");
        return Guid.Parse(value);
    }
}

// ── Request body records ──────────────────────────────────────────────────────

/// <summary>HTTP request body for POST /api/v1/users/me/enrollments/{accessId}/request-start.</summary>
/// <param name="RequestedStartDate">ISO-8601 date string (e.g. "2026-04-01").</param>
public record RequestStartDateRequest(string RequestedStartDate);

/// <summary>HTTP request body for PUT /api/v1/users/me.</summary>
/// <param name="FirstName">Updated first name.</param>
/// <param name="LastName">Updated last name.</param>
/// <param name="CountryCode">Optional dial code, e.g. "+91". Must be supplied with MobileNumber.</param>
/// <param name="MobileNumber">Optional mobile digits only. Must be supplied with CountryCode.</param>
/// <param name="WhatsAppOptIn">Whether to receive WhatsApp notifications.</param>
public record UpdateMyProfileRequest(
    string FirstName,
    string LastName,
    string? CountryCode,
    string? MobileNumber,
    bool WhatsAppOptIn);
