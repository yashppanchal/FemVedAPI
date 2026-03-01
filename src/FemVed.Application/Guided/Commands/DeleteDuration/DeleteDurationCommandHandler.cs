using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.DeleteDuration;

/// <summary>
/// Handles <see cref="DeleteDurationCommand"/>.
/// Sets <c>IsActive = false</c> on the duration (soft-deactivate — data is preserved).
/// Evicts the guided tree cache after saving.
/// </summary>
public sealed class DeleteDurationCommandHandler : IRequestHandler<DeleteDurationCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteDurationCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteDurationCommandHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramDuration> durations,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteDurationCommandHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _durations = durations;
        _uow       = uow;
        _cache     = cache;
        _logger    = logger;
    }

    /// <summary>Deactivates the duration and evicts the guided tree cache.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the duration or program is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller does not own the program.</exception>
    /// <exception cref="DomainException">Thrown when an Expert tries to modify a Published/Archived program.</exception>
    public async Task Handle(DeleteDurationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteDuration: user {UserId} deactivating duration {DurationId}",
            request.RequestingUserId, request.DurationId);

        var duration = await _durations.FirstOrDefaultAsync(
            d => d.Id == request.DurationId && d.ProgramId == request.ProgramId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(ProgramDuration), request.DurationId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Program), request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("You do not have an expert profile.");

            if (expert.Id != program.ExpertId)
                throw new ForbiddenException("You can only deactivate durations on your own programs.");

            if (program.Status is ProgramStatus.Published or ProgramStatus.Archived)
                throw new DomainException(
                    "Durations can only be deactivated on DRAFT or PENDING_REVIEW programs.");
        }

        duration.IsActive  = false;
        duration.UpdatedAt = DateTimeOffset.UtcNow;
        _durations.Update(duration);

        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("DeleteDuration: duration {DurationId} deactivated", request.DurationId);
    }
}
