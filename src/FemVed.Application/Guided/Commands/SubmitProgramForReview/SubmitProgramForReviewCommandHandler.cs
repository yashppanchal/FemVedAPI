using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.SubmitProgramForReview;

/// <summary>
/// Handles <see cref="SubmitProgramForReviewCommand"/>.
/// Validates ownership and transitions DRAFT → PENDING_REVIEW.
/// </summary>
public sealed class SubmitProgramForReviewCommandHandler : IRequestHandler<SubmitProgramForReviewCommand>
{
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<Domain.Entities.Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SubmitProgramForReviewCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public SubmitProgramForReviewCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<Domain.Entities.Expert> experts,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        IUnitOfWork uow,
        ILogger<SubmitProgramForReviewCommandHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _durations = durations;
        _prices    = prices;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Submits the program for Admin review.</summary>
    /// <param name="request">The submit command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the program is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when an Expert tries to submit another expert's program.</exception>
    /// <exception cref="DomainException">Thrown when the program is not in DRAFT status.</exception>
    public async Task Handle(SubmitProgramForReviewCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submit for review requested for program {ProgramId}", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted,
                cancellationToken)
                ?? throw new ForbiddenException("Expert profile not found.");

            if (program.ExpertId != expert.Id)
                throw new ForbiddenException("You can only submit your own programs for review.");
        }

        if (program.Status != ProgramStatus.Draft)
            throw new DomainException($"Only DRAFT programs can be submitted for review. Current status: {program.Status}.");

        // ── Fix 9: ensure at least one active duration with at least one active price exists ──
        var activeDurations = await _durations.GetAllAsync(
            d => d.ProgramId == request.ProgramId && d.IsActive, cancellationToken);

        if (activeDurations.Count == 0)
            throw new DomainException(
                "Program must have at least one active duration before it can be submitted for review.");

        var activeDurationIds = activeDurations.Select(d => d.Id).ToList();
        var hasPrices = await _prices.AnyAsync(
            p => activeDurationIds.Contains(p.DurationId) && p.IsActive, cancellationToken);

        if (!hasPrices)
            throw new DomainException(
                "Program must have at least one active price configured across its durations before it can be submitted for review.");

        program.Status = ProgramStatus.PendingReview;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Program {ProgramId} transitioned to PENDING_REVIEW", request.ProgramId);
    }
}
