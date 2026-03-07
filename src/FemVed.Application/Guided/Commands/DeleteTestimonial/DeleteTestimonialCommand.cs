using MediatR;

namespace FemVed.Application.Guided.Commands.DeleteTestimonial;

/// <summary>
/// Hides a testimonial by setting IsActive = false. The record is preserved in the database.
/// Experts can only delete testimonials on programs they own.
/// </summary>
/// <param name="TestimonialId">UUID of the testimonial to hide.</param>
/// <param name="ProgramId">UUID of the owning program (for ownership verification).</param>
/// <param name="RequestingUserId">Authenticated user's ID.</param>
/// <param name="IsAdmin">Whether the caller has Admin role.</param>
public record DeleteTestimonialCommand(
    Guid TestimonialId,
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin) : IRequest;
