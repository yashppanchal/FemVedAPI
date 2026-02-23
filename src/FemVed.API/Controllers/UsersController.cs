using System.Security.Claims;
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

    /// <summary>Returns the authenticated user's ID from the JWT NameIdentifier claim.</summary>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("NameIdentifier claim is missing from the JWT.");
        return Guid.Parse(value);
    }
}

// ── Request body records ──────────────────────────────────────────────────────

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
