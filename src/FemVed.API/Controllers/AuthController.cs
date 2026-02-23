using System.Security.Claims;
using FemVed.Application.Auth.Commands.ForgotPassword;
using FemVed.Application.Auth.Commands.Login;
using FemVed.Application.Auth.Commands.Logout;
using FemVed.Application.Auth.Commands.RefreshToken;
using FemVed.Application.Auth.Commands.Register;
using FemVed.Application.Auth.Commands.ResetPassword;
using FemVed.Application.Auth.Commands.VerifyEmail;
using FemVed.Application.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FemVed.API.Controllers;

/// <summary>
/// Handles all authentication operations: register, login, token refresh, logout,
/// forgot password, reset password, and email verification.
/// Base route: /api/v1/auth
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Registers a new user account (role = User).</summary>
    /// <param name="request">Registration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with auth tokens and user summary.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Authenticates a user and returns a new token pair.</summary>
    /// <param name="request">Email and password credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with auth tokens and user summary.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Rotates the refresh token: revokes the supplied token and issues a new pair.
    /// Pass the current (possibly expired) access token and the valid refresh token.
    /// </summary>
    /// <param name="request">Current access token and refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with new auth tokens.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Revokes the supplied refresh token, completing the logout.
    /// The short-lived access token naturally expires on its own.
    /// </summary>
    /// <param name="request">The refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content.</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID claim missing."));

        await _mediator.Send(new LogoutCommand(userId, request.RefreshToken), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Initiates the password reset flow by sending a reset link to the provided email.
    /// Always returns 200 regardless of whether the email exists (prevents enumeration).
    /// </summary>
    /// <param name="request">Email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK.</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(request, cancellationToken);
        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    /// <summary>
    /// Completes the password reset using the token from the email link.
    /// Revokes all existing refresh tokens on success.
    /// </summary>
    /// <param name="request">Reset token and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK.</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(request, cancellationToken);
        return Ok(new { message = "Password has been reset successfully. Please log in." });
    }

    /// <summary>
    /// Verifies the user's email address using the JWT from the verification link.
    /// Idempotent: calling again on an already-verified address is harmless.
    /// </summary>
    /// <param name="token">Email verification JWT from the link query string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK.</returns>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new VerifyEmailCommand(token), cancellationToken);
        return Ok(new { message = "Email address verified successfully." });
    }
}

/// <summary>Request body for the logout endpoint.</summary>
/// <param name="RefreshToken">The refresh token to revoke.</param>
public record LogoutRequest(string RefreshToken);
