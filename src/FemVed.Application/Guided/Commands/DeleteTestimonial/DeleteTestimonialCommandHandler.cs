using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.DeleteTestimonial;

/// <summary>
/// Handles <see cref="DeleteTestimonialCommand"/>.
/// Sets IsActive = false (soft-hide). The record is preserved.
/// Experts can only hide testimonials for programs they own.
/// </summary>
public sealed class DeleteTestimonialCommandHandler : IRequestHandler<DeleteTestimonialCommand>
{
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramTestimonial> _testimonials;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeleteTestimonialCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeleteTestimonialCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramTestimonial> testimonials,
        IUnitOfWork uow,
        ILogger<DeleteTestimonialCommandHandler> logger)
    {
        _programs     = programs;
        _experts      = experts;
        _testimonials = testimonials;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Hides the testimonial (sets IsActive = false).</summary>
    /// <exception cref="NotFoundException">Thrown when the testimonial or program does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when an expert tries to hide another expert's testimonial.</exception>
    public async Task Handle(DeleteTestimonialCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteTestimonial: hiding {TestimonialId}", request.TestimonialId);

        var testimonial = await _testimonials.FirstOrDefaultAsync(
            t => t.Id == request.TestimonialId && t.ProgramId == request.ProgramId, cancellationToken)
            ?? throw new NotFoundException("ProgramTestimonial", request.TestimonialId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("Expert profile not found.");

            if (program.ExpertId != expert.Id)
                throw new ForbiddenException("You can only hide testimonials for your own programs.");
        }

        testimonial.IsActive = false;
        _testimonials.Update(testimonial);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DeleteTestimonial: {TestimonialId} hidden", request.TestimonialId);
    }
}
