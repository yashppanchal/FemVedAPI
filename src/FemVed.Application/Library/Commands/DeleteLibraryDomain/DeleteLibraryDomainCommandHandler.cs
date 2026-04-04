using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.DeleteLibraryDomain;

/// <summary>
/// Handles <see cref="DeleteLibraryDomainCommand"/>.
/// Soft-deactivates the library domain by setting IsActive = false.
/// </summary>
public sealed class DeleteLibraryDomainCommandHandler : IRequestHandler<DeleteLibraryDomainCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryDomain> _domains;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteLibraryDomainCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteLibraryDomainCommandHandler(
        IRepository<LibraryDomain> domains,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteLibraryDomainCommandHandler> logger)
    {
        _domains = domains;
        _uow     = uow;
        _cache   = cache;
        _logger  = logger;
    }

    /// <summary>Soft-deactivates the library domain.</summary>
    /// <param name="request">The delete command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the domain does not exist.</exception>
    public async Task Handle(DeleteLibraryDomainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteLibraryDomain: deactivating domain {DomainId}", request.DomainId);

        var domain = await _domains.FirstOrDefaultAsync(
            d => d.Id == request.DomainId && d.IsActive, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryDomain), request.DomainId);

        domain.IsActive  = false;
        domain.UpdatedAt = DateTimeOffset.UtcNow;

        _domains.Update(domain);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("DeleteLibraryDomain: domain {DomainId} deactivated", domain.Id);
    }
}
