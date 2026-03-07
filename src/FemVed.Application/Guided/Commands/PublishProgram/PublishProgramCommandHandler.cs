using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.PublishProgram;

/// <summary>
/// Handles <see cref="PublishProgramCommand"/>.
/// Transitions PENDING_REVIEW → PUBLISHED and evicts the guided tree cache.
/// Guards that the program has at least one active duration, and that every
/// active duration has at least one active price, before allowing publish.
/// </summary>
public sealed class PublishProgramCommandHandler : IRequestHandler<PublishProgramCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PublishProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public PublishProgramCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<PublishProgramCommandHandler> logger)
    {
        _programs  = programs;
        _durations = durations;
        _prices    = prices;
        _uow       = uow;
        _cache     = cache;
        _logger    = logger;
    }

    /// <summary>
    /// Publishes the program and evicts the tree cache for all location codes.
    /// </summary>
    /// <param name="request">The publish command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the program is not found.</exception>
    /// <exception cref="DomainException">
    /// Thrown when the program is not in PENDING_REVIEW status, has no active durations,
    /// or any active duration has no active prices.
    /// </exception>
    public async Task Handle(PublishProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing program {ProgramId}", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (program.Status != ProgramStatus.PendingReview)
            throw new DomainException($"Only PENDING_REVIEW programs can be published. Current status: {program.Status}.");

        // ── Completeness guard: must have at least one active duration ─────────
        var activeDurations = await _durations.GetAllAsync(
            d => d.ProgramId == request.ProgramId && d.IsActive, cancellationToken);

        if (activeDurations.Count == 0)
            throw new DomainException(
                "Cannot publish a program with no active durations. Add at least one duration before publishing.");

        // ── Completeness guard: every active duration must have at least one active price ──
        foreach (var duration in activeDurations)
        {
            var hasPrices = await _prices.AnyAsync(
                p => p.DurationId == duration.Id && p.IsActive, cancellationToken);

            if (!hasPrices)
                throw new DomainException(
                    $"Duration '{duration.Label}' has no active prices. All active durations must have at least one active price before publishing.");
        }

        program.Status    = ProgramStatus.Published;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);
        await _uow.SaveChangesAsync(cancellationToken);

        // Evict tree cache for all known location codes so the new program appears immediately
        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("Program {ProgramId} published. Tree cache evicted.", request.ProgramId);
    }
}
