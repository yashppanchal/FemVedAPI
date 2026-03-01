using FemVed.Application.Guided.Commands.CreateProgram;
using MediatR;

namespace FemVed.Application.Guided.Commands.AddDuration;

/// <summary>
/// Adds a new duration option (with its location-specific prices) to an existing program.
/// Experts may only add durations to their own DRAFT or PENDING_REVIEW programs.
/// Admins may add durations to any program at any status.
/// </summary>
/// <param name="ProgramId">The program to add the duration to.</param>
/// <param name="RequestingUserId">Authenticated user ID — used to verify Expert ownership.</param>
/// <param name="IsAdmin">True when the caller has the Admin role.</param>
/// <param name="Label">Human-readable label, e.g. "4 weeks".</param>
/// <param name="Weeks">Number of weeks (for data integrity).</param>
/// <param name="SortOrder">Display ordering within the program.</param>
/// <param name="Prices">One or more location-specific prices for this duration.</param>
public record AddDurationCommand(
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    string Label,
    short Weeks,
    int SortOrder,
    List<DurationPriceInput> Prices) : IRequest<Guid>;
