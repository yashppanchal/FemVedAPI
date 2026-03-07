using FemVed.Application.Guided.DTOs;
using MediatR;

namespace FemVed.Application.Guided.Queries.GetProgramTestimonials;

/// <summary>
/// Returns all testimonials for a specific program (both active and inactive),
/// ordered by SortOrder ascending. Used by the expert/admin management dashboard.
/// </summary>
/// <param name="ProgramId">UUID of the program to list testimonials for.</param>
/// <param name="RequestingUserId">Authenticated user's ID (for ownership check when not admin).</param>
/// <param name="IsAdmin">Whether the caller has Admin role.</param>
public record GetProgramTestimonialsQuery(
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest<List<TestimonialDto>>;
