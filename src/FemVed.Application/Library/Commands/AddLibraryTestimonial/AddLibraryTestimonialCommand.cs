using MediatR;

namespace FemVed.Application.Library.Commands.AddLibraryTestimonial;

/// <summary>Adds a testimonial to a library video.</summary>
public record AddLibraryTestimonialCommand(
    Guid VideoId, string ReviewerName, string ReviewText,
    int Rating, int SortOrder) : IRequest<Guid>;
