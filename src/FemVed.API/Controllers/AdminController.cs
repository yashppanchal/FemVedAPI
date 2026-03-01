using System.Security.Claims;
using FemVed.Application.Admin.Commands.ActivateExpert;
using FemVed.Application.Admin.Commands.ActivateUser;
using FemVed.Application.Admin.Commands.ChangeUserRole;
using FemVed.Application.Admin.Commands.CreateCoupon;
using FemVed.Application.Admin.Commands.CreateExpert;
using FemVed.Application.Admin.Commands.DeactivateCoupon;
using FemVed.Application.Admin.Commands.DeactivateExpert;
using FemVed.Application.Admin.Commands.DeactivateUser;
using FemVed.Application.Admin.Commands.DeleteUser;
using FemVed.Application.Admin.Commands.ProcessGdprRequest;
using FemVed.Application.Admin.Commands.UpdateCoupon;
using FemVed.Application.Admin.Commands.UpdateExpert;
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
    /// <returns>200 OK with state confirmation payload.</returns>
    [HttpPut("users/{userId:guid}/activate")]
    [ProducesResponseType(typeof(AdminActivationResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ActivateUserCommand(userId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return Ok(new AdminActivationResultResponse(userId, true, true));
    }

    /// <summary>
    /// Deactivates the specified user account (sets IsActive = false).
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with state confirmation payload.</returns>
    [HttpPut("users/{userId:guid}/deactivate")]
    [ProducesResponseType(typeof(AdminActivationResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateUser(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeactivateUserCommand(userId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return Ok(new AdminActivationResultResponse(userId, false, true));
    }

    /// <summary>
    /// Soft-deletes the specified user account (sets IsDeleted = true, IsActive = false).
    /// The user is never hard-deleted.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with delete confirmation payload.</returns>
    [HttpDelete("users/{userId:guid}")]
    [ProducesResponseType(typeof(AdminDeleteResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeleteUserCommand(userId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return Ok(new AdminDeleteResultResponse(userId, true));
    }

    /// <summary>
    /// Changes the role of the specified user account.
    /// Pass roleId 1 = Admin, 2 = Expert (Coach), 3 = User.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="request">Request body containing the new role ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with update confirmation payload.</returns>
    [HttpPut("user/change-role")]
    [ProducesResponseType(typeof(AdminChangeRoleResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeUserRole(
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ChangeUserRoleCommand(request.UserId, request.RoleId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return Ok(new AdminChangeRoleResultResponse(request.UserId, request.RoleId, true));
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
    /// <returns>200 OK with state confirmation payload.</returns>
    [HttpPut("experts/{expertId:guid}/activate")]
    [ProducesResponseType(typeof(AdminActivationResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateExpert(Guid expertId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ActivateExpertCommand(expertId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return Ok(new AdminActivationResultResponse(expertId, true, true));
    }

    /// <summary>
    /// Deactivates the specified expert profile (sets IsActive = false).
    /// </summary>
    /// <param name="expertId">Target expert profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with state confirmation payload.</returns>
    [HttpPut("experts/{expertId:guid}/deactivate")]
    [ProducesResponseType(typeof(AdminActivationResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateExpert(Guid expertId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeactivateExpertCommand(expertId, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return Ok(new AdminActivationResultResponse(expertId, false, true));
    }

    /// <summary>
    /// Creates an expert profile for an existing user account.
    /// The user must already exist (registered). The profile is set active immediately.
    /// </summary>
    /// <param name="request">Expert profile details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the new expert profile ID.</returns>
    [HttpPost("experts")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateExpert(
        [FromBody] CreateExpertRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateExpertCommand(
                request.UserId,
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
                request.LocationCountry,
                GetCurrentUserId(),
                GetIpAddress()),
            cancellationToken);
        return CreatedAtAction(nameof(GetAllExperts), new { }, id);
    }

    /// <summary>
    /// Updates an existing expert profile. All fields are optional.
    /// Only non-null fields in the request body are applied.
    /// </summary>
    /// <param name="expertId">Target expert profile ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with update confirmation payload.</returns>
    [HttpPut("experts/{expertId:guid}")]
    [ProducesResponseType(typeof(AdminUpdateResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateExpert(
        Guid expertId,
        [FromBody] UpdateExpertRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpdateExpertCommand(
                expertId,
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
                request.LocationCountry,
                GetCurrentUserId(),
                GetIpAddress()),
            cancellationToken);
        return Ok(new AdminUpdateResultResponse(expertId, true));
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
                request.MinOrderAmount,
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
                request.MinOrderAmount,
                request.ClearMinOrderAmount,
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
    /// <returns>200 OK with state confirmation payload.</returns>
    [HttpPut("coupons/{couponId:guid}/deactivate")]
    [ProducesResponseType(typeof(AdminActivationResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCoupon(Guid couponId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new DeactivateCouponCommand(GetCurrentUserId(), GetIpAddress(), couponId),
            cancellationToken);
        return Ok(new AdminActivationResultResponse(couponId, false, true));
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
    /// <returns>200 OK with mutation confirmation payload.</returns>
    [HttpPost("gdpr-requests/{requestId:guid}/process")]
    [ProducesResponseType(typeof(AdminGdprProcessResultResponse), StatusCodes.Status200OK)]
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
        return Ok(new AdminGdprProcessResultResponse(requestId, request.Action.ToUpperInvariant(), true));
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

    /// <summary>Returns the authenticated admin's user ID from JWT claims (NameIdentifier or sub).</summary>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("User ID claim is missing from the JWT.");
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
    /// <summary>Minimum order amount (before discount) required. Null = no minimum.</summary>
    decimal? MinOrderAmount,
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
    /// <summary>New minimum order amount. Null = no change (unless ClearMinOrderAmount = true).</summary>
    decimal? MinOrderAmount,
    /// <summary>Set to true to explicitly clear MinOrderAmount (set to null).</summary>
    bool ClearMinOrderAmount,
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

/// <summary>HTTP request body for PUT /api/v1/admin/user/change-role.</summary>
public record ChangeUserRoleRequest(
    /// <summary>UUID of the user whose role will change.</summary>
    Guid UserId,
    /// <summary>New role ID: 1 = Admin, 2 = Expert (Coach), 3 = User.</summary>
    short RoleId);

/// <summary>HTTP request body for POST /api/v1/admin/experts.</summary>
public record CreateExpertRequest(
    /// <summary>ID of the existing user account to link as expert.</summary>
    Guid UserId,
    /// <summary>Public display name, e.g. "Dr. Prathima Nagesh".</summary>
    string DisplayName,
    /// <summary>Professional title, e.g. "Ayurvedic Physician".</summary>
    string Title,
    /// <summary>Full biography displayed on the program page.</summary>
    string Bio,
    /// <summary>Short bio for program grid cards (max 500 chars). Optional.</summary>
    string? GridDescription,
    /// <summary>Detailed long-form description. Optional.</summary>
    string? DetailedDescription,
    /// <summary>Profile photo URL. Optional.</summary>
    string? ProfileImageUrl,
    /// <summary>Grid card image URL. Optional.</summary>
    string? GridImageUrl,
    /// <summary>Areas of specialisation, e.g. ["Hormonal Health", "PCOS"]. Optional.</summary>
    List<string>? Specialisations,
    /// <summary>Years of clinical experience. Optional.</summary>
    short? YearsExperience,
    /// <summary>Degrees and certifications, e.g. ["BAMS", "MD Ayurveda"]. Optional.</summary>
    List<string>? Credentials,
    /// <summary>Country where the expert is based. Optional.</summary>
    string? LocationCountry);

/// <summary>HTTP request body for PUT /api/v1/admin/experts/{expertId}. All fields optional.</summary>
public record UpdateExpertRequest(
    /// <summary>New display name. Null = no change.</summary>
    string? DisplayName,
    /// <summary>New title. Null = no change.</summary>
    string? Title,
    /// <summary>New biography. Null = no change.</summary>
    string? Bio,
    /// <summary>New short grid description. Null = no change.</summary>
    string? GridDescription,
    /// <summary>New detailed description. Null = no change.</summary>
    string? DetailedDescription,
    /// <summary>New profile photo URL. Null = no change.</summary>
    string? ProfileImageUrl,
    /// <summary>New grid card image URL. Null = no change.</summary>
    string? GridImageUrl,
    /// <summary>Replaces all specialisations. Null = no change.</summary>
    List<string>? Specialisations,
    /// <summary>New years of experience. Null = no change.</summary>
    short? YearsExperience,
    /// <summary>Replaces all credentials. Null = no change.</summary>
    List<string>? Credentials,
    /// <summary>New country. Null = no change.</summary>
    string? LocationCountry);

/// <summary>HTTP request body for POST /api/v1/admin/gdpr-requests/{requestId}/process.</summary>
public record ProcessGdprRequestBody(
    /// <summary>"Complete" to fulfil the erasure, "Reject" to decline it.</summary>
    string Action,
    /// <summary>Required when Action is "Reject". Explains why the request was declined.</summary>
    string? RejectionReason);

/// <summary>Standard delete success payload returned by Admin DELETE endpoints.</summary>
/// <param name="Id">ID of the deleted resource.</param>
/// <param name="IsDeleted">Always true when deletion succeeds.</param>
public record AdminDeleteResultResponse(Guid Id, bool IsDeleted);

/// <summary>Standard update success payload returned by Admin PUT update endpoints.</summary>
/// <param name="Id">ID of the updated resource.</param>
/// <param name="IsUpdated">Always true when update succeeds.</param>
public record AdminUpdateResultResponse(Guid Id, bool IsUpdated);

/// <summary>Standard activation/deactivation payload returned by Admin state endpoints.</summary>
/// <param name="Id">ID of the resource whose active state changed.</param>
/// <param name="IsActive">Final active state after the operation.</param>
/// <param name="IsUpdated">Always true when state change succeeds.</param>
public record AdminActivationResultResponse(Guid Id, bool IsActive, bool IsUpdated);

/// <summary>Standard role-change payload returned by the user role update endpoint.</summary>
/// <param name="UserId">Target user ID.</param>
/// <param name="RoleId">Final role ID assigned to the user.</param>
/// <param name="IsUpdated">Always true when role update succeeds.</param>
public record AdminChangeRoleResultResponse(Guid UserId, short RoleId, bool IsUpdated);

/// <summary>Mutation confirmation payload returned by GDPR process endpoint.</summary>
/// <param name="RequestId">ID of the GDPR request processed.</param>
/// <param name="Action">Action applied: COMPLETE or REJECT.</param>
/// <param name="IsUpdated">Always true when processing succeeds.</param>
public record AdminGdprProcessResultResponse(Guid RequestId, string Action, bool IsUpdated);
