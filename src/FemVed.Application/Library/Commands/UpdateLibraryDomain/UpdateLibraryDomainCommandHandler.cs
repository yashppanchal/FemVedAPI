using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateLibraryDomain;

/// <summary>
/// Handles <see cref="UpdateLibraryDomainCommand"/>.
/// Applies partial updates to a library domain and invalidates the library tree cache.
/// </summary>
public sealed class UpdateLibraryDomainCommandHandler : IRequestHandler<UpdateLibraryDomainCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryDomain> _domains;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateLibraryDomainCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateLibraryDomainCommandHandler(
        IRepository<LibraryDomain> domains,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<UpdateLibraryDomainCommandHandler> logger)
    {
        _domains = domains;
        _uow     = uow;
        _cache   = cache;
        _logger  = logger;
    }

    /// <summary>Applies partial updates to the library domain.</summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the domain does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the requested slug is already taken by another domain.</exception>
    public async Task Handle(UpdateLibraryDomainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateLibraryDomain: updating domain {DomainId}", request.DomainId);

        var domain = await _domains.FirstOrDefaultAsync(
            d => d.Id == request.DomainId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryDomain), request.DomainId);

        // Validate slug uniqueness if slug is being changed
        if (request.Slug is not null && request.Slug != domain.Slug)
        {
            var slugTaken = await _domains.AnyAsync(
                d => d.Slug == request.Slug && d.Id != request.DomainId,
                cancellationToken);
            if (slugTaken)
                throw new DomainException($"A library domain with slug '{request.Slug}' already exists.");
        }

        if (request.Name is not null)             domain.Name             = request.Name.Trim();
        if (request.Slug is not null)             domain.Slug             = request.Slug.Trim().ToLowerInvariant();
        if (request.Description is not null)      domain.Description      = request.Description.Trim();
        if (request.HeroImageDesktop is not null) domain.HeroImageDesktop = request.HeroImageDesktop.Trim();
        if (request.HeroImageMobile is not null)  domain.HeroImageMobile  = request.HeroImageMobile.Trim();
        if (request.HeroImagePortrait is not null) domain.HeroImagePortrait = request.HeroImagePortrait.Trim();
        if (request.SortOrder is not null)        domain.SortOrder        = request.SortOrder.Value;
        if (request.IsActive is not null)         domain.IsActive         = request.IsActive.Value;
        domain.UpdatedAt = DateTimeOffset.UtcNow;

        _domains.Update(domain);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("UpdateLibraryDomain: domain {DomainId} updated", domain.Id);
    }
}
