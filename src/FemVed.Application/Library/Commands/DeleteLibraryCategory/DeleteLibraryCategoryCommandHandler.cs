using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.DeleteLibraryCategory;

/// <summary>
/// Handles <see cref="DeleteLibraryCategoryCommand"/>.
/// Soft-deactivates the library category by setting IsActive = false.
/// </summary>
public sealed class DeleteLibraryCategoryCommandHandler : IRequestHandler<DeleteLibraryCategoryCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryCategory> _categories;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteLibraryCategoryCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteLibraryCategoryCommandHandler(
        IRepository<LibraryCategory> categories,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteLibraryCategoryCommandHandler> logger)
    {
        _categories = categories;
        _uow        = uow;
        _cache       = cache;
        _logger      = logger;
    }

    /// <summary>Soft-deactivates the library category.</summary>
    /// <param name="request">The delete command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the category does not exist.</exception>
    public async Task Handle(DeleteLibraryCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteLibraryCategory: deactivating category {CategoryId}", request.CategoryId);

        var category = await _categories.FirstOrDefaultAsync(
            c => c.Id == request.CategoryId && c.IsActive, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryCategory), request.CategoryId);

        category.IsActive  = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        _categories.Update(category);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("DeleteLibraryCategory: category {CategoryId} deactivated", category.Id);
    }
}
