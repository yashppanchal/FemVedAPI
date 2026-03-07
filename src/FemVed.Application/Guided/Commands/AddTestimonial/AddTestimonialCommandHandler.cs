using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.AddTestimonial;

/// <summary>
/// Handles <see cref="AddTestimonialCommand"/>.
/// Creates a new testimonial row for the given program.
/// Experts can only add testimonials to programs they own.
/// </summary>
public sealed class AddTestimonialCommandHandler : IRequestHandler<AddTestimonialCommand, Guid>
{
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramTestimonial> _testimonials;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AddTestimonialCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public AddTestimonialCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramTestimonial> testimonials,
        IUnitOfWork uow,
        ILogger<AddTestimonialCommandHandler> logger)
    {
        _programs     = programs;
        _experts      = experts;
        _testimonials = testimonials;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Creates the testimonial and returns its new UUID.</summary>
    /// <exception cref="NotFoundException">Thrown when the program does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when an expert tries to add a testimonial to another expert's program.</exception>
    public async Task<Guid> Handle(AddTestimonialCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AddTestimonial: adding testimonial to program {ProgramId}", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("Expert profile not found.");

            if (program.ExpertId != expert.Id)
                throw new ForbiddenException("You can only add testimonials to your own programs.");
        }

        var testimonial = new ProgramTestimonial
        {
            Id            = Guid.NewGuid(),
            ProgramId     = request.ProgramId,
            ReviewerName  = request.ReviewerName.Trim(),
            ReviewerTitle = request.ReviewerTitle?.Trim(),
            ReviewText    = request.ReviewText.Trim(),
            Rating        = request.Rating,
            IsActive      = true,
            SortOrder     = request.SortOrder,
            CreatedAt     = DateTimeOffset.UtcNow
        };

        await _testimonials.AddAsync(testimonial);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("AddTestimonial: created {TestimonialId} for program {ProgramId}",
            testimonial.Id, request.ProgramId);

        return testimonial.Id;
    }
}
