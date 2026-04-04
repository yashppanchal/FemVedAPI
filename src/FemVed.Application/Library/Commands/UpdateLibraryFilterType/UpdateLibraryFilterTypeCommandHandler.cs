using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateLibraryFilterType;

/// <summary>Handles <see cref="UpdateLibraryFilterTypeCommand"/>.</summary>
public sealed class UpdateLibraryFilterTypeCommandHandler : IRequestHandler<UpdateLibraryFilterTypeCommand>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryFilterType> _filters;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateLibraryFilterTypeCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public UpdateLibraryFilterTypeCommandHandler(IRepository<LibraryFilterType> filters, IUnitOfWork uow, IMemoryCache cache, ILogger<UpdateLibraryFilterTypeCommandHandler> logger)
    { _filters = filters; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Updates the filter type.</summary>
    public async Task Handle(UpdateLibraryFilterTypeCommand request, CancellationToken cancellationToken)
    {
        var f = await _filters.FirstOrDefaultAsync(x => x.Id == request.FilterTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryFilterType), request.FilterTypeId);
        if (request.Name is not null) f.Name = request.Name.Trim();
        if (request.FilterKey is not null) f.FilterKey = request.FilterKey.Trim().ToLowerInvariant();
        if (request.FilterTarget is not null)
        {
            if (!Enum.TryParse<FilterTarget>(request.FilterTarget, true, out var target))
                throw new DomainException($"Invalid FilterTarget: {request.FilterTarget}.");
            f.FilterTarget = target;
        }
        if (request.SortOrder.HasValue) f.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) f.IsActive = request.IsActive.Value;
        _filters.Update(f);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
    }
}
