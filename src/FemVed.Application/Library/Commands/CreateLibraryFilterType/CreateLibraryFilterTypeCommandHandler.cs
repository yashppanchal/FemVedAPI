using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.CreateLibraryFilterType;

/// <summary>Handles <see cref="CreateLibraryFilterTypeCommand"/>.</summary>
public sealed class CreateLibraryFilterTypeCommandHandler : IRequestHandler<CreateLibraryFilterTypeCommand, Guid>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryDomain> _domains;
    private readonly IRepository<LibraryFilterType> _filters;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateLibraryFilterTypeCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public CreateLibraryFilterTypeCommandHandler(IRepository<LibraryDomain> domains, IRepository<LibraryFilterType> filters, IUnitOfWork uow, IMemoryCache cache, ILogger<CreateLibraryFilterTypeCommandHandler> logger)
    { _domains = domains; _filters = filters; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Creates the filter type.</summary>
    public async Task<Guid> Handle(CreateLibraryFilterTypeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating filter type {Name} for domain {DomainId}", request.Name, request.DomainId);
        if (!await _domains.AnyAsync(d => d.Id == request.DomainId, cancellationToken))
            throw new NotFoundException(nameof(LibraryDomain), request.DomainId);
        if (!Enum.TryParse<FilterTarget>(request.FilterTarget, true, out var target))
            throw new DomainException($"Invalid FilterTarget: {request.FilterTarget}. Valid values: VideoType, Category.");
        var filter = new LibraryFilterType
        {
            Id = Guid.NewGuid(), DomainId = request.DomainId,
            Name = request.Name.Trim(), FilterKey = request.FilterKey.Trim().ToLowerInvariant(),
            FilterTarget = target, SortOrder = request.SortOrder,
            IsActive = true, CreatedAt = DateTimeOffset.UtcNow
        };
        await _filters.AddAsync(filter);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
        _logger.LogInformation("Filter type {Id} created", filter.Id);
        return filter.Id;
    }
}
