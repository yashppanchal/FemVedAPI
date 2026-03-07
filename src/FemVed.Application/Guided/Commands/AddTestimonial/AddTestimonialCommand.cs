using MediatR;

namespace FemVed.Application.Guided.Commands.AddTestimonial;

/// <summary>
/// Adds a new testimonial to a program.
/// Experts can only add testimonials to their own programs.
/// </summary>
/// <param name="ProgramId">UUID of the program to add the testimonial to.</param>
/// <param name="RequestingUserId">Authenticated user's ID.</param>
/// <param name="IsAdmin">Whether the caller has Admin role.</param>
/// <param name="ReviewerName">Reviewer's display name.</param>
/// <param name="ReviewerTitle">Reviewer context, e.g. "Mother of two, London". Optional.</param>
/// <param name="ReviewText">The testimonial body text.</param>
/// <param name="Rating">Star rating 1–5. Optional.</param>
/// <param name="SortOrder">Display ordering position (default 0).</param>
public record AddTestimonialCommand(
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    string ReviewerName,
    string? ReviewerTitle,
    string ReviewText,
    short? Rating,
    int SortOrder) : IRequest<Guid>;
