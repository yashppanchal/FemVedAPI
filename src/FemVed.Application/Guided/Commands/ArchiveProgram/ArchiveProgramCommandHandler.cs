using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.ArchiveProgram;

/// <summary>
/// Handles <see cref="ArchiveProgramCommand"/>.
/// Transitions PUBLISHED → ARCHIVED and evicts the guided tree cache.
/// </summary>
public sealed class ArchiveProgramCommandHandler : IRequestHandler<ArchiveProgramCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ArchiveProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public ArchiveProgramCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<ArchiveProgramCommandHandler> logger)
    {
        _programs = programs;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>Archives the program and evicts the tree cache for all location codes.</summary>
    /// <param name="request">The archive command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the program is not found.</exception>
    /// <exception cref="DomainException">Thrown when the program is not in PUBLISHED status.</exception>
    public async Task Handle(ArchiveProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Archiving program {ProgramId}", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (program.Status != ProgramStatus.Published)
            throw new DomainException($"Only PUBLISHED programs can be archived. Current status: {program.Status}.");

        program.Status = ProgramStatus.Archived;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("Program {ProgramId} archived. Tree cache evicted.", request.ProgramId);
    }
}
