using System.Security.Claims;
using FemVed.Application.Payments.Commands.InitiateOrder;
using FemVed.Application.Payments.Commands.InitiateRefund;
using FemVed.Application.Payments.DTOs;
using FemVed.Application.Payments.Queries.GetMyOrders;
using FemVed.Application.Payments.Queries.GetOrder;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemVed.API.Controllers;

/// <summary>
/// Handles purchase order operations: initiating orders, viewing order history, and refunds.
/// Base route: /api/v1/orders
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Initialises the controller with MediatR.</summary>
    /// <param name="mediator">MediatR dispatcher.</param>
    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Initiates a purchase order for the authenticated user.
    /// Selects CashFree (IN) or PayPal (all other locations) based on the user's country.
    /// Duplicate <c>idempotencyKey</c> returns the existing order instead of creating a new one.
    /// </summary>
    /// <param name="request">Order initiation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with gateway-specific tokens (paymentSessionId or approvalUrl).</returns>
    [HttpPost("initiate")]
    [Authorize]
    [ProducesResponseType(typeof(InitiateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Initiate(
        [FromBody] InitiateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(
            new InitiateOrderCommand(userId, request.DurationId, request.CouponCode, request.IdempotencyKey),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Returns a single order by its ID.
    /// Users may only retrieve their own orders; Admins may retrieve any order.
    /// </summary>
    /// <param name="id">Order UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with order details, or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _mediator.Send(
            new GetOrderQuery(id, userId, isAdmin),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns all orders belonging to the authenticated user, newest first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with the list of orders (may be empty).</returns>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyOrdersQuery(userId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Initiates a refund for a paid order. Admin only.
    /// The refund amount must not exceed the original amount paid.
    /// </summary>
    /// <param name="id">Order UUID to refund.</param>
    /// <param name="request">Refund details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Refund(
        Guid id,
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(
            new InitiateRefundCommand(id, userId, request.RefundAmount, request.Reason),
            cancellationToken);
        return NoContent();
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

/// <summary>HTTP request body for POST /api/v1/orders/initiate.</summary>
/// <param name="DurationId">The program duration option to purchase.</param>
/// <param name="CouponCode">Optional discount coupon code.</param>
/// <param name="IdempotencyKey">Client-generated UUID to prevent duplicate orders.</param>
public record InitiateOrderRequest(Guid DurationId, string? CouponCode, string IdempotencyKey);

/// <summary>HTTP request body for POST /api/v1/orders/{id}/refund.</summary>
/// <param name="RefundAmount">Amount to refund (must be ≤ amount originally paid).</param>
/// <param name="Reason">Human-readable reason for the refund.</param>
public record RefundRequest(decimal RefundAmount, string Reason);
