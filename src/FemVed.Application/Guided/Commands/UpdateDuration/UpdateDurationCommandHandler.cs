using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.UpdateDuration;

/// <summary>
/// Handles <see cref="UpdateDurationCommand"/>.
/// Applies non-null field patches to a <see cref="ProgramDuration"/> row.
/// Evicts the guided tree cache after saving.
/// </summary>
public sealed class UpdateDurationCommandHandler : IRequestHandler<UpdateDurationCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateDurationCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateDurationCommandHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramDuration> durations,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<UpdateDurationCommandHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _durations = durations;
        _uow       = uow;
        _cache     = cache;
        _logger    = logger;
    }

    /// <summary>Patches the duration, verifying ownership and status, then evicts cache.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the duration or program is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller does not own the program.</exception>
    /// <exception cref="DomainException">Thrown when an Expert tries to modify a Published/Archived program.</exception>
    public async Task Handle(UpdateDurationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpdateDuration: user {UserId} updating duration {DurationId}",
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
                throw new ForbiddenException("You can only update durations on your own programs.");

            if (program.Status is ProgramStatus.Published or ProgramStatus.Archived)
                throw new DomainException(
                    "Durations can only be modified on DRAFT or PENDING_REVIEW programs.");
        }

        if (request.Label is not null)    duration.Label     = request.Label.Trim();
        if (request.Weeks.HasValue)       duration.Weeks     = request.Weeks.Value;
        if (request.SortOrder.HasValue)   duration.SortOrder = request.SortOrder.Value;
        duration.UpdatedAt = DateTimeOffset.UtcNow;

        _durations.Update(duration);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("UpdateDuration: duration {DurationId} updated", request.DurationId);
    }
}
