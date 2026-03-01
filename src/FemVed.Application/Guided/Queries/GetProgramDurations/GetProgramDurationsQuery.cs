using FemVed.Application.Guided.DTOs;
using MediatR;

namespace FemVed.Application.Guided.Queries.GetProgramDurations;

/// <summary>
/// Returns all durations for a program, each including ALL their location-specific prices.
/// Used by the expert / admin management dashboard — not filtered by location.
/// When <paramref name="DurationId"/> is provided, returns only that single duration.
/// </summary>
/// <param name="ProgramId">The program whose durations to fetch.</param>
/// <param name="RequestingUserId">Authenticated user ID — used to verify Expert ownership.</param>
/// <param name="IsAdmin">True when the caller has the Admin role (bypasses ownership check).</param>
/// <param name="DurationId">Optional. When set, returns only this duration.</param>
/// <param name="IsActive">Optional. When set, filters durations to only active (<c>true</c>) or only inactive (<c>false</c>). Null returns all.</param>
/// <param name="PriceIsActive">Optional. When set, filters each duration's price list to only active or only inactive prices. Null returns all.</param>
/// <param name="PriceLocationCode">Optional. When set, filters each duration's price list to the specified ISO country code (case-insensitive). Null returns all locations.</param>
public record GetProgramDurationsQuery(
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    Guid?   DurationId        = null,
    bool?   IsActive          = null,
    bool?   PriceIsActive     = null,
    string? PriceLocationCode = null) : IRequest<List<DurationManagementDto>>;
