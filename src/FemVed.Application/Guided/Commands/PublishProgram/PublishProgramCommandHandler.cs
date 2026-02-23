using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.PublishProgram;

/// <summary>
/// Handles <see cref="PublishProgramCommand"/>.
/// Transitions PENDING_REVIEW → PUBLISHED and evicts the guided tree cache.
/// </summary>
public sealed class PublishProgramCommandHandler : IRequestHandler<PublishProgramCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PublishProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public PublishProgramCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<PublishProgramCommandHandler> logger)
    {
        _programs = programs;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Publishes the program and evicts the tree cache for all location codes.</summary>
    /// <param name="request">The publish command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the program is not found.</exception>
    /// <exception cref="DomainException">Thrown when the program is not in PENDING_REVIEW status.</exception>
    public async Task Handle(PublishProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing program {ProgramId}", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (program.Status != ProgramStatus.PendingReview)
            throw new DomainException($"Only PENDING_REVIEW programs can be published. Current status: {program.Status}.");

        program.Status = ProgramStatus.Published;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);
        await _uow.SaveChangesAsync(cancellationToken);

        // Evict tree cache for all known location codes so the new program appears immediately
        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("Program {ProgramId} published. Tree cache evicted.", request.ProgramId);
    }
}
