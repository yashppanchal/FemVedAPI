using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.UpdateTestimonial;

/// <summary>
/// Handles <see cref="UpdateTestimonialCommand"/>.
/// Applies non-null patches to an existing testimonial.
/// Experts can only update testimonials for programs they own.
/// </summary>
public sealed class UpdateTestimonialCommandHandler : IRequestHandler<UpdateTestimonialCommand>
{
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramTestimonial> _testimonials;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateTestimonialCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public UpdateTestimonialCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramTestimonial> testimonials,
        IUnitOfWork uow,
        ILogger<UpdateTestimonialCommandHandler> logger)
    {
        _programs     = programs;
        _experts      = experts;
        _testimonials = testimonials;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Updates the testimonial.</summary>
    /// <exception cref="NotFoundException">Thrown when the testimonial or program does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when an expert tries to update another expert's testimonial.</exception>
    public async Task Handle(UpdateTestimonialCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateTestimonial: updating {TestimonialId}", request.TestimonialId);

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
                throw new ForbiddenException("You can only update testimonials for your own programs.");
        }

        if (request.ReviewerName is not null) testimonial.ReviewerName  = request.ReviewerName.Trim();
        if (request.ReviewerTitle is not null) testimonial.ReviewerTitle = request.ReviewerTitle.Trim();
        if (request.ReviewText is not null) testimonial.ReviewText       = request.ReviewText.Trim();
        if (request.Rating.HasValue)    testimonial.Rating     = request.Rating.Value;
        if (request.SortOrder.HasValue) testimonial.SortOrder  = request.SortOrder.Value;
        if (request.IsActive.HasValue)  testimonial.IsActive   = request.IsActive.Value;

        _testimonials.Update(testimonial);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("UpdateTestimonial: {TestimonialId} updated", request.TestimonialId);
    }
}
