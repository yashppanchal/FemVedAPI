using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.RestoreProgram;

/// <summary>
/// Handles <see cref="RestoreProgramCommand"/>.
/// Restores an archived or soft-deleted program back into the public catalog and
/// reactivates its durations. Throws when there is nothing to restore.
/// </summary>
public sealed class RestoreProgramCommandHandler : IRequestHandler<RestoreProgramCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RestoreProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public RestoreProgramCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<RestoreProgramCommandHandler> logger)
    {
        _programs  = programs;
        _durations = durations;
        _uow       = uow;
        _cache     = cache;
        _logger    = logger;
    }

    /// <summary>Restores the program.</summary>
    /// <exception cref="NotFoundException">Thrown when the program does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the program is neither archived nor soft-deleted.</exception>
    public async Task Handle(RestoreProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Restoring program {ProgramId}", request.ProgramId);

        // Note: the predicate intentionally does NOT exclude IsDeleted, because we want to
        // pick up soft-deleted programs and revive them.
        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId, cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        var wasSoftDeleted = program.IsDeleted;
        var wasArchived    = program.Status == ProgramStatus.Archived;

        if (!wasSoftDeleted && !wasArchived)
            throw new DomainException(
                $"Program is not archived or deleted (current status: {program.Status}). Nothing to restore.");

        if (wasSoftDeleted)
        {
            program.IsDeleted = false;
            program.IsActive  = true;
        }

        if (wasArchived)
            program.Status = ProgramStatus.Published;

        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);

        // If we are coming back from soft-delete, the delete handler had cascaded a
        // deactivation onto every duration — reactivate them all so the program is
        // immediately purchasable again. (Admin can re-disable individual durations later.)
        var reactivated = 0;
        if (wasSoftDeleted)
        {
            var durations = await _durations.GetAllAsync(
                d => d.ProgramId == request.ProgramId && !d.IsActive, cancellationToken);

            foreach (var dur in durations)
            {
                dur.IsActive  = true;
                dur.UpdatedAt = DateTimeOffset.UtcNow;
                _durations.Update(dur);
                reactivated++;
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "Program {ProgramId} restored (wasArchived={WasArchived}, wasSoftDeleted={WasSoftDeleted}, durationsReactivated={Reactivated})",
            request.ProgramId, wasArchived, wasSoftDeleted, reactivated);
    }
}
