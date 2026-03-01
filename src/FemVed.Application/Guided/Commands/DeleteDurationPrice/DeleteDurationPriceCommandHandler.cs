using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.DeleteDurationPrice;

/// <summary>
/// Handles <see cref="DeleteDurationPriceCommand"/>.
/// Sets <c>IsActive = false</c> on the price row (soft-deactivate — data is preserved).
/// Verifies the price → duration → program ownership chain before saving.
/// Evicts the guided tree cache after saving.
/// </summary>
public sealed class DeleteDurationPriceCommandHandler : IRequestHandler<DeleteDurationPriceCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteDurationPriceCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteDurationPriceCommandHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteDurationPriceCommandHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _durations = durations;
        _prices    = prices;
        _uow       = uow;
        _cache     = cache;
        _logger    = logger;
    }

    /// <summary>Deactivates the price row after verifying the ownership chain, then evicts the cache.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the price, duration, or program is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller does not own the program.</exception>
    /// <exception cref="DomainException">Thrown when an Expert tries to modify a Published/Archived program.</exception>
    public async Task Handle(DeleteDurationPriceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeleteDurationPrice: user {UserId} deactivating price {PriceId}",
            request.RequestingUserId, request.PriceId);

        // Verify price → duration chain
        var price = await _prices.FirstOrDefaultAsync(
            p => p.Id == request.PriceId && p.DurationId == request.DurationId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(DurationPrice), request.PriceId);

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
                throw new ForbiddenException("You can only deactivate prices on your own programs.");

            if (program.Status is ProgramStatus.Published or ProgramStatus.Archived)
                throw new DomainException(
                    "Prices can only be deactivated on DRAFT or PENDING_REVIEW programs.");
        }

        price.IsActive  = false;
        price.UpdatedAt = DateTimeOffset.UtcNow;
        _prices.Update(price);

        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("DeleteDurationPrice: price {PriceId} deactivated", request.PriceId);
    }
}
