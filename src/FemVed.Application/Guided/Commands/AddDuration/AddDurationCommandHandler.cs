using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.AddDuration;

/// <summary>
/// Handles <see cref="AddDurationCommand"/>.
/// Creates a new <see cref="ProgramDuration"/> row and its associated <see cref="DurationPrice"/> rows.
/// Evicts the guided tree cache after saving.
/// </summary>
public sealed class AddDurationCommandHandler : IRequestHandler<AddDurationCommand, Guid>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<DurationPrice> _prices;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AddDurationCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public AddDurationCommandHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramDuration> durations,
        IRepository<DurationPrice> prices,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<AddDurationCommandHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _durations = durations;
        _prices    = prices;
        _uow       = uow;
        _cache     = cache;
        _logger    = logger;
    }

    /// <summary>Creates the duration and its prices, then evicts the guided tree cache.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new duration's primary key.</returns>
    /// <exception cref="NotFoundException">Thrown when the program is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller does not own the program.</exception>
    /// <exception cref="DomainException">Thrown when an Expert tries to modify a Published/Archived program.</exception>
    public async Task<Guid> Handle(AddDurationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "AddDuration: user {UserId} adding duration '{Label}' to program {ProgramId}",
            request.RequestingUserId, request.Label, request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Program), request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("You do not have an expert profile.");

            if (expert.Id != program.ExpertId)
                throw new ForbiddenException("You can only add durations to your own programs.");

            if (program.Status is ProgramStatus.Published or ProgramStatus.Archived)
                throw new DomainException(
                    "Durations can only be added to DRAFT or PENDING_REVIEW programs.");
        }

        var duration = new ProgramDuration
        {
            Id        = Guid.NewGuid(),
            ProgramId = request.ProgramId,
            Label     = request.Label.Trim(),
            Weeks     = request.Weeks,
            SortOrder = request.SortOrder,
            IsActive  = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _durations.AddAsync(duration);

        foreach (var p in request.Prices)
        {
            await _prices.AddAsync(new DurationPrice
            {
                Id             = Guid.NewGuid(),
                DurationId     = duration.Id,
                LocationCode   = p.LocationCode.ToUpperInvariant(),
                Amount         = p.Amount,
                CurrencyCode   = p.CurrencyCode.ToUpperInvariant(),
                CurrencySymbol = p.CurrencySymbol,
                IsActive       = true,
                CreatedAt      = DateTimeOffset.UtcNow,
                UpdatedAt      = DateTimeOffset.UtcNow
            });
        }

        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "AddDuration: duration {DurationId} added to program {ProgramId}",
            duration.Id, request.ProgramId);

        return duration.Id;
    }
}
