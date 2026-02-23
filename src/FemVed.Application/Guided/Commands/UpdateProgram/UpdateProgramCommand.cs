using MediatR;

namespace FemVed.Application.Guided.Commands.UpdateProgram;

/// <summary>
/// Updates an existing program. Experts may only update their own DRAFT or PENDING_REVIEW programs.
/// All content fields are optional — only non-null values are applied.
/// </summary>
/// <param name="ProgramId">The program to update.</param>
/// <param name="RequestingUserId">Authenticated user ID for ownership verification.</param>
/// <param name="IsAdmin">True when the caller is an Admin (bypasses ownership check).</param>
/// <param name="Name">New program name (optional).</param>
/// <param name="GridDescription">New grid description (optional).</param>
/// <param name="GridImageUrl">New grid image URL (optional).</param>
/// <param name="Overview">New overview text (optional).</param>
/// <param name="SortOrder">New sort order (optional).</param>
public record UpdateProgramCommand(
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    string? Name,
    string? GridDescription,
    string? GridImageUrl,
    string? Overview,
    int? SortOrder) : IRequest;
