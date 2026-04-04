using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetAllLibraryCategories;

/// <summary>Handles <see cref="GetAllLibraryCategoriesQuery"/>.</summary>
public sealed class GetAllLibraryCategoriesQueryHandler
    : IRequestHandler<GetAllLibraryCategoriesQuery, List<AdminLibraryCategoryDto>>
{
    private readonly IRepository<LibraryCategory> _categories;
    private readonly IRepository<LibraryVideo> _videos;
    private readonly ILogger<GetAllLibraryCategoriesQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllLibraryCategoriesQueryHandler(IRepository<LibraryCategory> categories, IRepository<LibraryVideo> videos, ILogger<GetAllLibraryCategoriesQueryHandler> logger)
    { _categories = categories; _videos = videos; _logger = logger; }

    /// <summary>Returns all categories.</summary>
    public async Task<List<AdminLibraryCategoryDto>> Handle(GetAllLibraryCategoriesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading all library categories for admin");
        var categories = await _categories.GetAllAsync(null, cancellationToken);
        var videos = await _videos.GetAllAsync(null, cancellationToken);
        var vidCounts = videos.GroupBy(v => v.CategoryId).ToDictionary(g => g.Key, g => g.Count());
        return categories.OrderBy(c => c.SortOrder)
            .Select(c => new AdminLibraryCategoryDto(c.Id, c.DomainId, c.Name, c.Slug, c.SortOrder, c.IsActive, vidCounts.GetValueOrDefault(c.Id, 0)))
            .ToList();
    }
}
