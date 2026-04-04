using MediatR;

namespace FemVed.Application.Library.Commands.DeleteLibraryTestimonial;

/// <summary>Removes a testimonial from a library video.</summary>
public record DeleteLibraryTestimonialCommand(Guid TestimonialId) : IRequest;
