using MediatR;

namespace FemVed.Application.Guided.Commands.UpdateTestimonial;

/// <summary>
/// Updates an existing testimonial. All payload fields are optional; only non-null values are applied.
/// Experts can only update testimonials on programs they own.
/// </summary>
/// <param name="TestimonialId">UUID of the testimonial to update.</param>
/// <param name="ProgramId">UUID of the owning program (for ownership verification).</param>
/// <param name="RequestingUserId">Authenticated user's ID.</param>
/// <param name="IsAdmin">Whether the caller has Admin role.</param>
/// <param name="ReviewerName">New reviewer display name. Null = no change.</param>
/// <param name="ReviewerTitle">New reviewer context. Null = no change.</param>
/// <param name="ReviewText">New testimonial body text. Null = no change.</param>
/// <param name="Rating">New star rating 1–5. Null = no change.</param>
/// <param name="SortOrder">New display order. Null = no change.</param>
/// <param name="IsActive">New visibility flag. Null = no change.</param>
public record UpdateTestimonialCommand(
    Guid TestimonialId,
    Guid ProgramId,
    Guid RequestingUserId,
    bool IsAdmin,
    string? ReviewerName,
    string? ReviewerTitle,
    string? ReviewText,
    short? Rating,
    int? SortOrder,
    bool? IsActive) : IRequest;
