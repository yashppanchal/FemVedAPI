using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.UpdateCategory;

/// <summary>
/// Handles <see cref="UpdateCategoryCommand"/>.
/// Applies partial updates to a category. When WhatsIncluded or KeyAreas are provided,
/// existing child records are replaced entirely.
/// </summary>
public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
{
    private readonly IRepository<GuidedCategory> _categories;
    private readonly IRepository<CategoryWhatsIncluded> _whatsIncluded;
    private readonly IRepository<CategoryKeyArea> _keyAreas;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateCategoryCommandHandler(
        IRepository<GuidedCategory> categories,
        IRepository<CategoryWhatsIncluded> whatsIncluded,
        IRepository<CategoryKeyArea> keyAreas,
        IUnitOfWork uow,
        ILogger<UpdateCategoryCommandHandler> logger)
    {
        _categories = categories;
        _whatsIncluded = whatsIncluded;
        _keyAreas = keyAreas;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Applies partial updates to the category.</summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the category ID does not exist.</exception>
    public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating category {CategoryId}", request.CategoryId);

        var category = await _categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(GuidedCategory), request.CategoryId);

        if (request.Name is not null) category.Name = request.Name.Trim();
        if (request.CategoryType is not null) category.CategoryType = request.CategoryType.Trim();
        if (request.HeroTitle is not null) category.HeroTitle = request.HeroTitle.Trim();
        if (request.HeroSubtext is not null) category.HeroSubtext = request.HeroSubtext.Trim();
        if (request.CtaLabel is not null) category.CtaLabel = request.CtaLabel.Trim();
        if (request.CtaLink is not null) category.CtaLink = request.CtaLink.Trim();
        if (request.PageHeader is not null) category.PageHeader = request.PageHeader.Trim();
        if (request.ImageUrl is not null) category.ImageUrl = request.ImageUrl.Trim();
        if (request.SortOrder is not null) category.SortOrder = request.SortOrder.Value;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        _categories.Update(category);

        if (request.WhatsIncluded is not null)
        {
            var existing = await _whatsIncluded.GetAllAsync(w => w.CategoryId == category.Id, cancellationToken);
            foreach (var item in existing) _whatsIncluded.Remove(item);
            for (var i = 0; i < request.WhatsIncluded.Count; i++)
                await _whatsIncluded.AddAsync(new CategoryWhatsIncluded
                {
                    Id = Guid.NewGuid(), CategoryId = category.Id,
                    ItemText = request.WhatsIncluded[i].Trim(), SortOrder = i
                });
        }

        if (request.KeyAreas is not null)
        {
            var existing = await _keyAreas.GetAllAsync(k => k.CategoryId == category.Id, cancellationToken);
            foreach (var item in existing) _keyAreas.Remove(item);
            for (var i = 0; i < request.KeyAreas.Count; i++)
                await _keyAreas.AddAsync(new CategoryKeyArea
                {
                    Id = Guid.NewGuid(), CategoryId = category.Id,
                    AreaText = request.KeyAreas[i].Trim(), SortOrder = i
                });
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Category {CategoryId} updated", request.CategoryId);
    }
}
