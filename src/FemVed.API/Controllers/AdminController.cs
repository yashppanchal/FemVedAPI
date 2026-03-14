using System.Security.Claims;
using FemVed.Application.Enrollments.Commands.EndEnrollment;
using FemVed.Application.Enrollments.Commands.PauseEnrollment;
using FemVed.Application.Enrollments.Commands.ResumeEnrollment;
using FemVed.Application.Enrollments.Commands.StartEnrollment;
using FemVed.Application.Experts.Commands.SendProgressUpdate;
using FemVed.Application.Experts.DTOs;
using FemVed.Application.Experts.Queries.GetEnrollmentComments;
using FemVed.Application.Admin.Commands.ActivateExpert;
using FemVed.Application.Admin.Commands.ActivateUser;
using FemVed.Application.Admin.Commands.ChangeUserRole;
using FemVed.Application.Admin.Commands.CreateCoupon;
using FemVed.Application.Admin.Commands.CreateExpert;
using FemVed.Application.Admin.Commands.ActivateCoupon;
using FemVed.Application.Admin.Commands.DeactivateCoupon;
using FemVed.Application.Admin.Commands.DeactivateExpert;
using FemVed.Application.Admin.Commands.DeactivateUser;
using FemVed.Application.Admin.Commands.ChangeUserEmail;
using FemVed.Application.Admin.Commands.DeleteUser;
using FemVed.Application.Admin.Commands.ProcessGdprRequest;
using FemVed.Application.Admin.Commands.UpdateCoupon;
using FemVed.Application.Admin.Commands.UpdateExpert;
using FemVed.Application.Admin.Commands.UpsertExpertByUserId;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Admin.Queries.GetAdminSummary;
using FemVed.Application.Admin.Queries.GetAllCoupons;
using FemVed.Application.Admin.Queries.GetAllExperts;
using FemVed.Application.Admin.Queries.GetAllEnrollments;
using FemVed.Application.Admin.Queries.GetAllOrders;
using FemVed.Application.Admin.Queries.GetAllPrograms;
using FemVed.Application.Admin.Queries.GetAllUsers;
using FemVed.Application.Admin.Queries.GetAuditLog;
using FemVed.Application.Admin.Queries.GetGdprRequests;
using FemVed.Application.Admin.Queries.GetSalesAnalytics;
using FemVed.Application.Admin.Queries.GetProgramAnalytics;
using FemVed.Application.Admin.Queries.GetUserAnalytics;
using FemVed.Application.Admin.Queries.GetExpertPayoutAnalytics;
using FemVed.Application.Admin.Queries.GetExpertPayoutHistory;
using FemVed.Application.Admin.Queries.GetExpertPayoutBalance;
using FemVed.Application.Admin.Commands.RecordExpertPayout;
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

    /// <summary>
    /// Changes the email address of the specified user account.
    /// Validates the new email is not already taken and writes an audit log entry.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="request">Request body containing the new email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the updated user record.</returns>
    [HttpPut("users/{userId:guid}/email")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeUserEmail(
        Guid userId,
        [FromBody] ChangeUserEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ChangeUserEmailCommand(userId, request.NewEmail, GetCurrentUserId(), GetIpAddress()),
            cancellationToken);
        return Ok(result);
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
                request.CommissionRate,
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
                request.CommissionRate,
                GetCurrentUserId(),
                GetIpAddress()),
            cancellationToken);
        return Ok(new AdminUpdateResultResponse(expertId, true));
    }

    /// <summary>
    /// Creates or updates the expert profile for a given user account, identified by user ID.
    /// Designed to be called immediately after promoting a user to the Expert role.
    /// The role change auto-creates a minimal profile; this endpoint enriches it with
    /// admin-supplied details. All fields are optional — only non-null values are applied.
    /// </summary>
    /// <param name="userId">The user whose expert profile will be created or updated.</param>
    /// <param name="request">Profile fields to set. All optional.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("users/{userId:guid}/expert-profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpsertExpertProfile(
        Guid userId,
        [FromBody] UpsertExpertProfileRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UpsertExpertByUserIdCommand(
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
                request.LocationCountry,
                GetCurrentUserId(),
                GetIpAddress()),
            cancellationToken);
        return Ok();
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

    /// <summary>
    /// Reactivates a previously deactivated coupon (sets IsActive = true).
    /// </summary>
    /// <param name="couponId">Target coupon ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with state confirmation payload; 422 if the coupon is already active.</returns>
    [HttpPut("coupons/{couponId:guid}/activate")]
    [ProducesResponseType(typeof(AdminActivationResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ActivateCoupon(Guid couponId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ActivateCouponCommand(GetCurrentUserId(), GetIpAddress(), couponId),
            cancellationToken);
        return Ok(new AdminActivationResultResponse(couponId, true, true));
    }

    // ── Orders ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all orders across all users, newest first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of orders.</returns>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(List<AdminOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllOrders(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllOrdersQuery(), cancellationToken);
        return Ok(result);
    }

    // ── Programs (Admin) ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns all programs across all experts, including DRAFT, PENDING_REVIEW, PUBLISHED, and ARCHIVED.
    /// This is the primary entry point for the admin review workflow.
    /// Optionally filter by lifecycle status.
    /// </summary>
    /// <param name="status">
    /// Optional lifecycle status filter: Draft, PendingReview, Published, or Archived.
    /// Omit to return all statuses.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the flat list of programs.</returns>
    [HttpGet("programs")]
    [ProducesResponseType(typeof(List<AdminProgramDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllPrograms(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllProgramsQuery(status), cancellationToken);
        return Ok(result);
    }

    // ── Enrollments (Admin) ────────────────────────────────────────────────────

    /// <summary>
    /// Returns all enrollments across all experts, newest first.
    /// Required to discover <c>accessId</c> values for session management actions
    /// (start, pause, resume, end).
    /// Optionally filter by access status.
    /// </summary>
    /// <param name="status">
    /// Optional access-status filter: NotStarted, Active, Paused, Completed, or Cancelled.
    /// Omit to return all statuses.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the flat list of enrollments.</returns>
    [HttpGet("enrollments")]
    [ProducesResponseType(typeof(List<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllEnrollments(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllEnrollmentsQuery(status), cancellationToken);
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

    // ── Enrollment Session Management (Admin) ─────────────────────────────────

    /// <summary>
    /// Starts an enrollment on behalf of any expert — transitions it from NOT_STARTED to ACTIVE.
    /// Emails the enrolled user a <c>session_started</c> notification.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record to start.</param>
    /// <param name="request">Optional note to log against this action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("enrollments/{accessId:guid}/start")]
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
            new StartEnrollmentCommand(accessId, userId, IsAdmin: true, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "started"));
    }

    /// <summary>
    /// Pauses an enrollment — transitions it from ACTIVE to PAUSED.
    /// Emails the enrolled user a <c>session_paused</c> notification.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record to pause.</param>
    /// <param name="request">Optional note to log against this action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("enrollments/{accessId:guid}/pause")]
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
            new PauseEnrollmentCommand(accessId, userId, IsAdmin: true, IsUser: false, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "paused"));
    }

    /// <summary>
    /// Resumes a paused enrollment — transitions it from PAUSED back to ACTIVE.
    /// Emails the enrolled user a <c>session_resumed</c> notification.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record to resume.</param>
    /// <param name="request">Optional note to log against this action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("enrollments/{accessId:guid}/resume")]
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
            new ResumeEnrollmentCommand(accessId, userId, IsAdmin: true, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "resumed"));
    }

    /// <summary>
    /// Ends an enrollment — transitions it from ACTIVE or PAUSED to COMPLETED.
    /// Emails the enrolled user a <c>session_ended</c> notification.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record to end.</param>
    /// <param name="request">Optional note to log against this action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPost("enrollments/{accessId:guid}/end")]
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
            new EndEnrollmentCommand(accessId, userId, IsAdmin: true, IsUser: false, request?.Note),
            cancellationToken);
        return Ok(new SessionActionResponse(accessId, "ended"));
    }

    // ── Enrollment Comments (Admin) ───────────────────────────────────────────

    /// <summary>
    /// Sends a progress comment as an admin to a specific enrolled user.
    /// Always dispatches an email via SendGrid (<c>expert_progress_update</c> template).
    /// Admins may comment on any enrollment regardless of which expert owns the program.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record.</param>
    /// <param name="request">The comment text (10–2000 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with confirmation payload.</returns>
    [HttpPost("enrollments/{accessId:guid}/comments")]
    [ProducesResponseType(typeof(CommentSentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendComment(
        Guid accessId,
        [FromBody] AdminSendCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(
            new SendProgressUpdateCommand(userId, accessId, request.UpdateNote, IsAdmin: true),
            cancellationToken);
        return Ok(new CommentSentResponse(accessId, true));
    }

    /// <summary>
    /// Returns all comments sent for a specific enrollment, oldest first.
    /// Admins may view comments for any enrollment.
    /// </summary>
    /// <param name="accessId">UUID of the UserProgramAccess record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with list of comments (may be empty).</returns>
    [HttpGet("enrollments/{accessId:guid}/comments")]
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
            new GetEnrollmentCommentsQuery(accessId, userId, IsAdmin: true),
            cancellationToken);
        return Ok(result);
    }

    // ── Analytics ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns aggregated sales analytics: revenue by currency, gateway, country, and monthly trends.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with sales analytics data.</returns>
    [HttpGet("analytics/sales")]
    [ProducesResponseType(typeof(SalesAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSalesAnalytics(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSalesAnalyticsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns per-program and per-expert performance analytics including revenue, enrollments,
    /// expert share, and platform commission.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with program analytics data.</returns>
    [HttpGet("analytics/programs")]
    [ProducesResponseType(typeof(ProgramAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProgramAnalytics(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProgramAnalyticsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns user acquisition and cohort analytics: registration trends, buyer conversion rates,
    /// repeat purchase ratios, and 30/60/90-day cohort purchase rates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with user analytics data.</returns>
    [HttpGet("analytics/users")]
    [ProducesResponseType(typeof(UserAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserAnalytics(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserAnalyticsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns expert payout balances: total earned, expert share, total paid, and outstanding balance
    /// for every expert, grouped by currency.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with list of expert payout balance summaries.</returns>
    [HttpGet("analytics/expert-payouts")]
    [ProducesResponseType(typeof(List<ExpertPayoutBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetExpertPayoutAnalytics(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExpertPayoutAnalyticsQuery(), cancellationToken);
        return Ok(result);
    }

    // ── Expert Payouts ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the complete financial summary for a single expert:
    /// total revenue collected from their programs, their payout share (commissionRate %),
    /// platform profit, amount already paid, and outstanding balance — all per currency.
    /// Use this as the primary "pay this expert" dashboard view.
    /// </summary>
    /// <param name="expertId">Target expert profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the expert's full financial balance sheet.</returns>
    [HttpGet("expert-payouts/{expertId:guid}/balance")]
    [ProducesResponseType(typeof(ExpertPayoutBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpertPayoutBalance(
        Guid expertId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetExpertPayoutBalanceQuery(expertId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns all payout records for a specific expert, newest first.
    /// </summary>
    /// <param name="expertId">Target expert profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of payout records (may be empty).</returns>
    [HttpGet("expert-payouts/{expertId:guid}")]
    [ProducesResponseType(typeof(List<ExpertPayoutRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExpertPayoutHistory(
        Guid expertId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetExpertPayoutHistoryQuery(expertId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Records a payment made from the platform to an expert.
    /// Creates one row in <c>expert_payouts</c> and writes an audit log entry.
    /// The outstanding balance is always computed dynamically (earned minus paid).
    /// </summary>
    /// <param name="request">Payout details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with the payout record.</returns>
    [HttpPost("expert-payouts")]
    [ProducesResponseType(typeof(ExpertPayoutRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordExpertPayout(
        [FromBody] RecordExpertPayoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RecordExpertPayoutCommand(
                request.ExpertId,
                request.Amount,
                request.CurrencyCode,
                request.PaidAt,
                request.PaymentReference,
                request.Notes,
                GetCurrentUserId(),
                GetIpAddress()),
            cancellationToken);
        return CreatedAtAction(nameof(GetExpertPayoutHistory), new { expertId = result.ExpertId }, result);
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
    string? LocationCountry,
    /// <summary>Expert commission rate as a percentage. Defaults to 80.00 (expert earns 80% of revenue).</summary>
    decimal CommissionRate = 80.00m);

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
    string? LocationCountry,
    /// <summary>New commission rate as a percentage, e.g. 75.00. Null = no change.</summary>
    decimal? CommissionRate);

/// <summary>HTTP request body for POST /api/v1/admin/users/{userId}/expert-profile. All fields optional.</summary>
public record UpsertExpertProfileRequest(
    /// <summary>Public display name. Null = no change.</summary>
    string? DisplayName,
    /// <summary>Professional title. Null = no change.</summary>
    string? Title,
    /// <summary>Full biography. Null = no change.</summary>
    string? Bio,
    /// <summary>Short bio for grid cards (max 500 chars). Null = no change.</summary>
    string? GridDescription,
    /// <summary>Detailed long-form description. Null = no change.</summary>
    string? DetailedDescription,
    /// <summary>Profile photo URL. Null = no change.</summary>
    string? ProfileImageUrl,
    /// <summary>Grid card image URL. Null = no change.</summary>
    string? GridImageUrl,
    /// <summary>Areas of specialisation. Null = no change.</summary>
    List<string>? Specialisations,
    /// <summary>Years of clinical experience. Null = no change.</summary>
    short? YearsExperience,
    /// <summary>Degrees and certifications. Null = no change.</summary>
    List<string>? Credentials,
    /// <summary>Country where the expert is based. Null = no change.</summary>
    string? LocationCountry);

/// <summary>HTTP request body for POST /api/v1/admin/expert-payouts.</summary>
public record RecordExpertPayoutRequest(
    /// <summary>UUID of the expert receiving the payment.</summary>
    Guid ExpertId,
    /// <summary>Amount transferred. Must be greater than 0.</summary>
    decimal Amount,
    /// <summary>ISO 4217 currency code, e.g. "GBP".</summary>
    string CurrencyCode,
    /// <summary>UTC timestamp when the funds were actually transferred.</summary>
    DateTimeOffset PaidAt,
    /// <summary>Optional bank wire ref, PayPal transaction ID, etc. Max 255 chars.</summary>
    string? PaymentReference,
    /// <summary>Optional admin notes about this payment.</summary>
    string? Notes);

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

/// <summary>HTTP request body for POST /api/v1/admin/enrollments/{accessId}/comments.</summary>
/// <param name="UpdateNote">The comment text (10–2000 characters).</param>
public record AdminSendCommentRequest(string UpdateNote);

/// <summary>HTTP request body for PUT /api/v1/admin/users/{userId}/email.</summary>
/// <param name="NewEmail">The new email address to assign to the user account.</param>
public record ChangeUserEmailRequest(string NewEmail);
