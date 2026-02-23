using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.CreateCategory;

/// <summary>
/// Handles <see cref="CreateCategoryCommand"/>.
/// Creates a new category with its WhatsIncluded and KeyArea child records.
/// </summary>
public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
{
    private readonly IRepository<GuidedDomain> _domains;
    private readonly IRepository<GuidedCategory> _categories;
    private readonly IRepository<CategoryWhatsIncluded> _whatsIncluded;
    private readonly IRepository<CategoryKeyArea> _keyAreas;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public CreateCategoryCommandHandler(
        IRepository<GuidedDomain> domains,
        IRepository<GuidedCategory> categories,
        IRepository<CategoryWhatsIncluded> whatsIncluded,
        IRepository<CategoryKeyArea> keyAreas,
        IUnitOfWork uow,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _domains = domains;
        _categories = categories;
        _whatsIncluded = whatsIncluded;
        _keyAreas = keyAreas;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Creates a new category with all child records.</summary>
    /// <param name="request">The create category command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new category's primary key.</returns>
    /// <exception cref="NotFoundException">Thrown when the domain ID does not exist.</exception>
    /// <exception cref="ValidationException">Thrown when the slug is already in use.</exception>
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating category with slug {Slug} in domain {DomainId}", request.Slug, request.DomainId);

        var domainExists = await _domains.AnyAsync(d => d.Id == request.DomainId, cancellationToken);
        if (!domainExists)
            throw new NotFoundException(nameof(GuidedDomain), request.DomainId);

        var slugExists = await _categories.AnyAsync(c => c.Slug == request.Slug, cancellationToken);
        if (slugExists)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "slug", [$"A category with slug '{request.Slug}' already exists."] }
            });

        var category = new GuidedCategory
        {
            Id = Guid.NewGuid(),
            DomainId = request.DomainId,
            ParentId = request.ParentId,
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            CategoryType = request.CategoryType.Trim(),
            HeroTitle = request.HeroTitle.Trim(),
            HeroSubtext = request.HeroSubtext.Trim(),
            CtaLabel = request.CtaLabel?.Trim(),
            CtaLink = request.CtaLink?.Trim(),
            PageHeader = request.PageHeader?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _categories.AddAsync(category);

        for (var i = 0; i < request.WhatsIncluded.Count; i++)
        {
            await _whatsIncluded.AddAsync(new CategoryWhatsIncluded
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                ItemText = request.WhatsIncluded[i].Trim(),
                SortOrder = i
            });
        }

        for (var i = 0; i < request.KeyAreas.Count; i++)
        {
            await _keyAreas.AddAsync(new CategoryKeyArea
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                AreaText = request.KeyAreas[i].Trim(),
                SortOrder = i
            });
        }

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category {CategoryId} created with slug {Slug}", category.Id, category.Slug);
        return category.Id;
    }
}
