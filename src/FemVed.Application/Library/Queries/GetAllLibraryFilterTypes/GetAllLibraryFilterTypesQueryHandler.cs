using FemVed.Application.Interfaces;
using FemVed.Application.Library.DTOs;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Library.Queries.GetAllLibraryFilterTypes;

/// <summary>Handles <see cref="GetAllLibraryFilterTypesQuery"/>.</summary>
public sealed class GetAllLibraryFilterTypesQueryHandler
    : IRequestHandler<GetAllLibraryFilterTypesQuery, List<AdminFilterTypeDto>>
{
    private readonly IRepository<LibraryFilterType> _filters;
    private readonly ILogger<GetAllLibraryFilterTypesQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAllLibraryFilterTypesQueryHandler(IRepository<LibraryFilterType> filters, ILogger<GetAllLibraryFilterTypesQueryHandler> logger)
    { _filters = filters; _logger = logger; }

    /// <summary>Returns all filter types.</summary>
    public async Task<List<AdminFilterTypeDto>> Handle(GetAllLibraryFilterTypesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading all library filter types for admin");
        var filters = await _filters.GetAllAsync(null, cancellationToken);
        return filters.OrderBy(f => f.SortOrder)
            .Select(f => new AdminFilterTypeDto(f.Id, f.DomainId, f.Name, f.FilterKey, f.FilterTarget.ToString().ToUpperInvariant(), f.SortOrder, f.IsActive))
            .ToList();
    }
}
