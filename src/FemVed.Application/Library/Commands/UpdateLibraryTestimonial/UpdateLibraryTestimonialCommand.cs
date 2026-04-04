using MediatR;

namespace FemVed.Application.Library.Commands.UpdateLibraryTestimonial;

/// <summary>Updates a library video testimonial.</summary>
public record UpdateLibraryTestimonialCommand(
    Guid TestimonialId, string? ReviewerName, string? ReviewText,
    int? Rating, int? SortOrder, bool? IsActive) : IRequest;
