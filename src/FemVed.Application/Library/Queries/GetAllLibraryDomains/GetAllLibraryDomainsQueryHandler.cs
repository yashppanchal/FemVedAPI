using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetAllLibraryDomains;

/// <summary>Handles <see cref="GetAllLibraryDomainsQuery"/>.</summary>
public sealed class GetAllLibraryDomainsQueryHandler
    : IRequestHandler<GetAllLibraryDomainsQuery, List<AdminLibraryDomainDto>>
{
    private readonly IRepository<LibraryDomain> _domains;
    private readonly IRepository<LibraryCategory> _categories;
    private readonly ILogger<GetAllLibraryDomainsQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllLibraryDomainsQueryHandler(IRepository<LibraryDomain> domains, IRepository<LibraryCategory> categories, ILogger<GetAllLibraryDomainsQueryHandler> logger)
    { _domains = domains; _categories = categories; _logger = logger; }

    /// <summary>Returns all domains.</summary>
    public async Task<List<AdminLibraryDomainDto>> Handle(GetAllLibraryDomainsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading all library domains for admin");
        var domains = await _domains.GetAllAsync(null, cancellationToken);
        var categories = await _categories.GetAllAsync(null, cancellationToken);
        var catCounts = categories.GroupBy(c => c.DomainId).ToDictionary(g => g.Key, g => g.Count());
        return domains.OrderBy(d => d.SortOrder)
            .Select(d => new AdminLibraryDomainDto(d.Id, d.Name, d.Slug, d.SortOrder, d.IsActive, catCounts.GetValueOrDefault(d.Id, 0)))
            .ToList();
    }
}
