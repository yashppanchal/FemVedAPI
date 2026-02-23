using System.Security.Claims;
using FemVed.Application.Admin.Commands.ActivateExpert;
using FemVed.Application.Admin.Commands.ActivateUser;
using FemVed.Application.Admin.Commands.CreateCoupon;
using FemVed.Application.Admin.Commands.DeactivateCoupon;
using FemVed.Application.Admin.Commands.DeactivateExpert;
using FemVed.Application.Admin.Commands.DeactivateUser;
using FemVed.Application.Admin.Commands.DeleteUser;
using FemVed.Application.Admin.Commands.ProcessGdprRequest;
using FemVed.Application.Admin.Commands.UpdateCoupon;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Admin.Queries.GetAdminSummary;
using FemVed.Application.Admin.Queries.GetAllCoupons;
using FemVed.Application.Admin.Queries.GetAllExperts;
using FemVed.Application.Admin.Queries.GetAllOrders;
using FemVed.Application.Admin.Queries.GetAllUsers;
using FemVed.Application.Admin.Queries.GetAuditLog;
using FemVed.Application.Admin.Queries.GetGdprRequests;
using FemVed.Application.Payments.DTOs;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Admin-only operations: user/expert management, coupons, orders, GDPR, audit log, and summary stats.
/// All endpoints require the <c>AdminOnly</c> policy.
/// Base route: /api/v1/admin
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Summary ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns aggregated platform statistics for the admin dashboard summary card.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with platform statistics.</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(AdminSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAdminSummaryQuery(), cancellationToken);
        return Ok(result);
    }

    // ── Users ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all user accounts (including soft-deleted), newest first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of users.</returns>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllUsersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Activates the specified user account (sets IsActive = true).
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("users/{userId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ActivateUserCommand(userId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deactivates the specified user account (sets IsActive = false).
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("users/{userId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeactivateUserCommand(userId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes the specified user account (sets IsDeleted = true, IsActive = false).
    /// The user is never hard-deleted.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteUserCommand(userId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return NoContent();
    }

    // ── Experts ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all expert profiles (including soft-deleted), newest first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of expert profiles.</returns>
    [HttpGet("experts")]
    [ProducesResponseType(typeof(List<AdminExpertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllExperts(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllExpertsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Activates the specified expert profile (sets IsActive = true).
    /// </summary>
    /// <param name="expertId">Target expert profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("experts/{expertId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateExpert(Guid expertId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ActivateExpertCommand(expertId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deactivates the specified expert profile (sets IsActive = false).
    /// </summary>
    /// <param name="expertId">Target expert profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("experts/{expertId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateExpert(Guid expertId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeactivateExpertCommand(expertId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return NoContent();
    }

    // ── Coupons ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all coupon records, newest first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of coupons.</returns>
    [HttpGet("coupons")]
    [ProducesResponseType(typeof(List<CouponDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllCoupons(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllCouponsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new discount coupon.
    /// </summary>
    /// <param name="request">Coupon creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new coupon.</returns>
    [HttpPost("coupons")]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateCoupon(
        [FromBody] CreateCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateCouponCommand(
                GetCurrentUserId(),
                GetIpAddress(),
                request.Code,
                request.DiscountType,
                request.DiscountValue,
                request.MaxUses,
                request.ValidFrom,
                request.ValidUntil),
            cancellationToken);
        return CreatedAtAction(nameof(GetAllCoupons), result);
    }

    /// <summary>
    /// Updates an existing coupon. Only non-null fields in the request are applied.
    /// Supply <c>clearMaxUses = true</c> / <c>clearValidFrom = true</c> / <c>clearValidUntil = true</c> to explicitly null those fields.
    /// </summary>
    /// <param name="couponId">Target coupon ID.</param>
    /// <param name="request">Partial update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the updated coupon.</returns>
    [HttpPut("coupons/{couponId:guid}")]
    [ProducesResponseType(typeof(CouponDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateCoupon(
        Guid couponId,
        [FromBody] UpdateCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateCouponCommand(
                GetCurrentUserId(),
                GetIpAddress(),
                couponId,
                request.Code,
                request.DiscountType,
                request.DiscountValue,
                request.MaxUses,
                request.ClearMaxUses,
                request.ValidFrom,
                request.ClearValidFrom,
                request.ValidUntil,
                request.ClearValidUntil),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deactivates the specified coupon (sets IsActive = false). The coupon remains in the database.
    /// </summary>
    /// <param name="couponId">Target coupon ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPut("coupons/{couponId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCoupon(Guid couponId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeactivateCouponCommand(GetCurrentUserId(), GetIpAddress(), couponId),
            cancellationToken);
        return NoContent();
    }

    // ── Orders ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all orders across all users, newest first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of orders.</returns>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllOrdersQuery(), cancellationToken);
        return Ok(result);
    }

    // ── GDPR ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns GDPR erasure requests. Defaults to Pending requests only.
    /// Pass <c>status=null</c> (omit the query parameter) to return all.
    /// </summary>
    /// <param name="status">Optional status filter. Omit to return all statuses.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of GDPR requests.</returns>
    [HttpGet("gdpr-requests")]
    [ProducesResponseType(typeof(List<GdprRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGdprRequests(
        [FromQuery] GdprRequestStatus? status = GdprRequestStatus.Pending,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetGdprRequestsQuery(status), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Marks a GDPR erasure request as Completed or Rejected.
    /// A rejection reason is required when the action is "Reject".
    /// </summary>
    /// <param name="requestId">Target GDPR request ID.</param>
    /// <param name="request">Action payload: "Complete" or "Reject" with optional reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("gdpr-requests/{requestId:guid}/process")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ProcessGdprRequest(
        Guid requestId,
        [FromBody] ProcessGdprRequestBody request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ProcessGdprRequestCommand(
                GetCurrentUserId(),
                GetIpAddress(),
                requestId,
                request.Action,
                request.RejectionReason),
            cancellationToken);
        return NoContent();
    }

    // ── Audit Log ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the most recent audit log entries, newest first.
    /// </summary>
    /// <param name="limit">Maximum number of entries to return. Defaults to 100.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of audit log entries.</returns>
    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAuditLogQuery(limit), cancellationToken);
        return Ok(result);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Returns the authenticated admin's user ID from the JWT NameIdentifier claim.</summary>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("NameIdentifier claim is missing from the JWT.");
        return Guid.Parse(value);
    }

    /// <summary>Returns the client IP address from the HTTP context, or null if unavailable.</summary>
    private string? GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}

// ── Request body records ──────────────────────────────────────────────────────

/// <summary>HTTP request body for POST /api/v1/admin/coupons.</summary>
public record CreateCouponRequest(
    /// <summary>Uppercase coupon code, e.g. "WELCOME20".</summary>
    string Code,
    /// <summary>Whether the discount is Percentage or Flat.</summary>
    DiscountType DiscountType,
    /// <summary>Discount value: percentage (0–100) or flat amount.</summary>
    decimal DiscountValue,
    /// <summary>Maximum number of redemptions. Null = unlimited.</summary>
    int? MaxUses,
    /// <summary>UTC start of validity window. Null = valid immediately.</summary>
    DateTimeOffset? ValidFrom,
    /// <summary>UTC end of validity window. Null = never expires.</summary>
    DateTimeOffset? ValidUntil);

/// <summary>HTTP request body for PUT /api/v1/admin/coupons/{couponId}.</summary>
public record UpdateCouponRequest(
    /// <summary>New coupon code. Null = no change.</summary>
    string? Code,
    /// <summary>New discount type. Null = no change.</summary>
    DiscountType? DiscountType,
    /// <summary>New discount value. Null = no change.</summary>
    decimal? DiscountValue,
    /// <summary>New max uses value. Null = no change (unless ClearMaxUses = true).</summary>
    int? MaxUses,
    /// <summary>Set to true to explicitly clear MaxUses (set to null).</summary>
    bool ClearMaxUses,
    /// <summary>New ValidFrom. Null = no change (unless ClearValidFrom = true).</summary>
    DateTimeOffset? ValidFrom,
    /// <summary>Set to true to explicitly clear ValidFrom (set to null).</summary>
    bool ClearValidFrom,
    /// <summary>New ValidUntil. Null = no change (unless ClearValidUntil = true).</summary>
    DateTimeOffset? ValidUntil,
    /// <summary>Set to true to explicitly clear ValidUntil (set to null).</summary>
    bool ClearValidUntil);

/// <summary>HTTP request body for POST /api/v1/admin/gdpr-requests/{requestId}/process.</summary>
public record ProcessGdprRequestBody(
    /// <summary>"Complete" to fulfil the erasure, "Reject" to decline it.</summary>
    string Action,
    /// <summary>Required when Action is "Reject". Explains why the request was declined.</summary>
    string? RejectionReason);
