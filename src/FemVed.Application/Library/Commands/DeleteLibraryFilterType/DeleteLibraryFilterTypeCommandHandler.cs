using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.DeleteLibraryFilterType;

/// <summary>Handles <see cref="DeleteLibraryFilterTypeCommand"/>.</summary>
public sealed class DeleteLibraryFilterTypeCommandHandler : IRequestHandler<DeleteLibraryFilterTypeCommand>
{
    private static readonly string[] Locations = ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];
    private readonly IRepository<LibraryFilterType> _filters;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteLibraryFilterTypeCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeleteLibraryFilterTypeCommandHandler(IRepository<LibraryFilterType> filters, IUnitOfWork uow, IMemoryCache cache, ILogger<DeleteLibraryFilterTypeCommandHandler> logger)
    { _filters = filters; _uow = uow; _cache = cache; _logger = logger; }

    /// <summary>Removes the filter type.</summary>
    public async Task Handle(DeleteLibraryFilterTypeCommand request, CancellationToken cancellationToken)
    {
        var f = await _filters.FirstOrDefaultAsync(x => x.Id == request.FilterTypeId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryFilterType), request.FilterTypeId);
        _filters.Remove(f);
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var loc in Locations) _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");
    }
}
