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
public record GetProgramDurationsQuery(
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    Guid? DurationId = null) : IRequest<List<DurationManagementDto>>;
