using FemVed.Application.Guided.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Queries.GetProgramTestimonials;

/// <summary>
/// Handles <see cref="GetProgramTestimonialsQuery"/>.
/// Returns all testimonials (active and inactive) for the given program.
/// Experts can only view testimonials for programs they own.
/// </summary>
public sealed class GetProgramTestimonialsQueryHandler
    : IRequestHandler<GetProgramTestimonialsQuery, List<TestimonialDto>>
{
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramTestimonial> _testimonials;
    private readonly ILogger<GetProgramTestimonialsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetProgramTestimonialsQueryHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramTestimonial> testimonials,
        ILogger<GetProgramTestimonialsQueryHandler> logger)
    {
        _programs     = programs;
        _experts      = experts;
        _testimonials = testimonials;
        _logger       = logger;
    }

    /// <summary>Returns all testimonials for the program, ordered by SortOrder ascending.</summary>
    /// <exception cref="NotFoundException">Thrown when the program does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when an expert tries to view another expert's testimonials.</exception>
    public async Task<List<TestimonialDto>> Handle(
        GetProgramTestimonialsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetProgramTestimonials: loading for program {ProgramId}", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("Expert profile not found.");

            if (program.ExpertId != expert.Id)
                throw new ForbiddenException("You can only view testimonials for your own programs.");
        }

        var testimonials = await _testimonials.GetAllAsync(
            t => t.ProgramId == request.ProgramId, cancellationToken);

        var result = testimonials
            .OrderBy(t => t.SortOrder)
            .Select(t => new TestimonialDto(
                TestimonialId: t.Id,
                ProgramId:     t.ProgramId,
                ReviewerName:  t.ReviewerName,
                ReviewerTitle: t.ReviewerTitle,
                ReviewText:    t.ReviewText,
                Rating:        t.Rating,
                IsActive:      t.IsActive,
                SortOrder:     t.SortOrder,
                CreatedAt:     t.CreatedAt))
            .ToList();

        _logger.LogInformation("GetProgramTestimonials: returned {Count} for program {ProgramId}",
            result.Count, request.ProgramId);

        return result;
    }
}
