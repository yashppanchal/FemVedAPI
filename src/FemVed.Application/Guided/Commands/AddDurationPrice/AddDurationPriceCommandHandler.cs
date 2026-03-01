using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.AddDurationPrice;

/// <summary>
/// Handles <see cref="AddDurationPriceCommand"/>.
/// Creates a new <see cref="DurationPrice"/> row for a specific location.
/// Guards against duplicate active prices for the same location on the same duration.
/// Evicts the guided tree cache after saving.
/// </summary>
public sealed class AddDurationPriceCommandHandler : IRequestHandler<AddDurationPriceCommand, Guid>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AddDurationPriceCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public AddDurationPriceCommandHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<AddDurationPriceCommandHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _durations = durations;
        _prices    = prices;
        _uow       = uow;
        _cache     = cache;
        _logger    = logger;
    }

    /// <summary>Creates the price, enforces the duplicate guard, then evicts the cache.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new price row's primary key.</returns>
    /// <exception cref="NotFoundException">Thrown when the duration or program is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller does not own the program.</exception>
    /// <exception cref="DomainException">Thrown when an active price for the same location already exists,
    /// or when an Expert tries to modify a Published/Archived program.</exception>
    public async Task<Guid> Handle(AddDurationPriceCommand request, CancellationToken cancellationToken)
    {
        var locationCode = request.LocationCode.ToUpperInvariant();

        _logger.LogInformation(
            "AddDurationPrice: user {UserId} adding {LocationCode} price to duration {DurationId}",
            request.RequestingUserId, locationCode, request.DurationId);

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
                throw new ForbiddenException("You can only add prices to durations on your own programs.");

            if (program.Status is ProgramStatus.Published or ProgramStatus.Archived)
                throw new DomainException(
                    "Prices can only be added on DRAFT or PENDING_REVIEW programs.");
        }

        // Guard: only one active price per location per duration
        var duplicate = await _prices.AnyAsync(
            p => p.DurationId == request.DurationId
              && p.LocationCode == locationCode
              && p.IsActive,
            cancellationToken);

        if (duplicate)
            throw new DomainException(
                $"An active price for location '{locationCode}' already exists on this duration. " +
                "Update the existing price instead, or deactivate it first.");

        var price = new DurationPrice
        {
            Id             = Guid.NewGuid(),
            DurationId     = duration.Id,
            LocationCode   = locationCode,
            Amount         = request.Amount,
            CurrencyCode   = request.CurrencyCode.ToUpperInvariant(),
            CurrencySymbol = request.CurrencySymbol,
            IsActive       = true,
            CreatedAt      = DateTimeOffset.UtcNow,
            UpdatedAt      = DateTimeOffset.UtcNow
        };

        await _prices.AddAsync(price);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "AddDurationPrice: price {PriceId} ({LocationCode}) added to duration {DurationId}",
            price.Id, locationCode, request.DurationId);

        return price.Id;
    }
}
