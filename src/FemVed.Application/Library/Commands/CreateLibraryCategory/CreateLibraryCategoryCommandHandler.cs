using FemVed.Application.Interfaces;
using FemVed.Application.Library.Queries.GetLibraryTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Commands.CreateLibraryCategory;

/// <summary>
/// Handles <see cref="CreateLibraryCategoryCommand"/>.
/// Creates a new library category, returns its ID, and invalidates the library tree cache.
/// </summary>
public sealed class CreateLibraryCategoryCommandHandler : IRequestHandler<CreateLibraryCategoryCommand, Guid>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<LibraryDomain> _domains;
    private readonly IRepository<LibraryCategory> _categories;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CreateLibraryCategoryCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public CreateLibraryCategoryCommandHandler(
        IRepository<LibraryDomain> domains,
        IRepository<LibraryCategory> categories,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<CreateLibraryCategoryCommandHandler> logger)
    {
        _domains    = domains;
        _categories = categories;
        _uow        = uow;
        _cache       = cache;
        _logger      = logger;
    }

    /// <summary>Creates a new library category and invalidates the library tree cache.</summary>
    /// <param name="request">The create library category command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new category's primary key.</returns>
    /// <exception cref="NotFoundException">Thrown when the domain ID does not exist.</exception>
    /// <exception cref="ValidationException">Thrown when the slug is already in use.</exception>
    public async Task<Guid> Handle(CreateLibraryCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateLibraryCategory: creating category with slug {Slug} in domain {DomainId}",
            request.Slug, request.DomainId);

        var domainExists = await _domains.AnyAsync(d => d.Id == request.DomainId && d.IsActive, cancellationToken);
        if (!domainExists)
            throw new NotFoundException(nameof(LibraryDomain), request.DomainId);

        var slugExists = await _categories.AnyAsync(c => c.Slug == request.Slug, cancellationToken);
        if (slugExists)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "slug", [$"A library category with slug '{request.Slug}' already exists."] }
            });

        var category = new LibraryCategory
        {
            Id          = Guid.NewGuid(),
            DomainId    = request.DomainId,
            Name        = request.Name.Trim(),
            Slug        = request.Slug.Trim().ToLowerInvariant(),
            Description = request.Description?.Trim(),
            CardImage   = request.CardImage?.Trim(),
            SortOrder   = request.SortOrder,
            IsActive    = true,
            CreatedAt   = DateTimeOffset.UtcNow,
            UpdatedAt   = DateTimeOffset.UtcNow
        };

        await _categories.AddAsync(category);
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetLibraryTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("CreateLibraryCategory: category {CategoryId} created with slug {Slug}",
            category.Id, category.Slug);
        return category.Id;
    }
}
