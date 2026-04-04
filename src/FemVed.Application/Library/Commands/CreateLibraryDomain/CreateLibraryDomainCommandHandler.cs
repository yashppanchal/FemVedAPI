using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.CreateLibraryDomain;

/// <summary>
/// Handles <see cref="CreateLibraryDomainCommand"/>.
/// Creates a new library domain, returns its ID, and invalidates the library tree cache.
/// </summary>
public sealed class CreateLibraryDomainCommandHandler : IRequestHandler<CreateLibraryDomainCommand, Guid>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryDomain> _domains;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateLibraryDomainCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public CreateLibraryDomainCommandHandler(
        IRepository<LibraryDomain> domains,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<CreateLibraryDomainCommandHandler> logger)
    {
        _domains = domains;
        _uow     = uow;
        _cache   = cache;
        _logger  = logger;
    }

    /// <summary>Creates a new library domain and invalidates the library tree cache.</summary>
    /// <param name="request">The create library domain command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new domain's primary key.</returns>
    /// <exception cref="ValidationException">Thrown when the slug is already in use.</exception>
    public async Task<Guid> Handle(CreateLibraryDomainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateLibraryDomain: creating domain with slug {Slug}", request.Slug);

        var slugExists = await _domains.AnyAsync(d => d.Slug == request.Slug, cancellationToken);
        if (slugExists)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "slug", [$"A library domain with slug '{request.Slug}' already exists."] }
            });

        var domain = new LibraryDomain
        {
            Id               = Guid.NewGuid(),
            Name             = request.Name.Trim(),
            Slug             = request.Slug.Trim().ToLowerInvariant(),
            Description      = request.Description?.Trim(),
            HeroImageDesktop = request.HeroImageDesktop?.Trim(),
            HeroImageMobile  = request.HeroImageMobile?.Trim(),
            HeroImagePortrait = request.HeroImagePortrait?.Trim(),
            SortOrder        = request.SortOrder,
            IsActive         = true,
            CreatedAt        = DateTimeOffset.UtcNow,
            UpdatedAt        = DateTimeOffset.UtcNow
        };

        await _domains.AddAsync(domain);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("CreateLibraryDomain: domain {DomainId} created with slug {Slug}", domain.Id, domain.Slug);
        return domain.Id;
    }
}
