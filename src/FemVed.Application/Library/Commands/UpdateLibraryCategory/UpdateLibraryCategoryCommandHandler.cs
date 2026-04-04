using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.UpdateLibraryCategory;

/// <summary>
/// Handles <see cref="UpdateLibraryCategoryCommand"/>.
/// Applies partial updates to a library category and invalidates the library tree cache.
/// </summary>
public sealed class UpdateLibraryCategoryCommandHandler : IRequestHandler<UpdateLibraryCategoryCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryCategory> _categories;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateLibraryCategoryCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateLibraryCategoryCommandHandler(
        IRepository<LibraryCategory> categories,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<UpdateLibraryCategoryCommandHandler> logger)
    {
        _categories = categories;
        _uow        = uow;
        _cache       = cache;
        _logger      = logger;
    }

    /// <summary>Applies partial updates to the library category.</summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the category does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the requested slug is already taken by another category.</exception>
    public async Task Handle(UpdateLibraryCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateLibraryCategory: updating category {CategoryId}", request.CategoryId);

        var category = await _categories.FirstOrDefaultAsync(
            c => c.Id == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(LibraryCategory), request.CategoryId);

        // Validate slug uniqueness if slug is being changed
        if (request.Slug is not null && request.Slug != category.Slug)
        {
            var slugTaken = await _categories.AnyAsync(
                c => c.Slug == request.Slug && c.Id != request.CategoryId,
                cancellationToken);
            if (slugTaken)
                throw new DomainException($"A library category with slug '{request.Slug}' already exists.");
        }

        if (request.Name is not null)      category.Name        = request.Name.Trim();
        if (request.Slug is not null)      category.Slug        = request.Slug.Trim().ToLowerInvariant();
        if (request.Description is not null) category.Description = request.Description.Trim();
        if (request.CardImage is not null) category.CardImage   = request.CardImage.Trim();
        if (request.SortOrder is not null) category.SortOrder   = request.SortOrder.Value;
        if (request.IsActive is not null)  category.IsActive    = request.IsActive.Value;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        _categories.Update(category);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("UpdateLibraryCategory: category {CategoryId} updated", category.Id);
    }
}
