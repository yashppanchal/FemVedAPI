using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace FemVed.Application.Guided.Commands.DeleteCategory;

/// <summary>
/// Handles <see cref="DeleteCategoryCommand"/>.
/// Sets IsDeleted = true, IsActive = false on the category, and writes an audit log entry.
/// </summary>
public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<GuidedCategory> _categories;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteCategoryCommandHandler(
        IRepository<GuidedCategory> categories,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteCategoryCommandHandler> logger)
    {
        _categories = categories;
        _auditLogs  = auditLogs;
        _uow        = uow;
        _cache      = cache;
        _logger     = logger;
    }

    /// <summary>Soft-deletes the category and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the category does not exist or is already deleted.</exception>
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteCategory: admin {AdminId} soft-deleting category {CategoryId}",
            request.AdminUserId, request.CategoryId);

        var category = await _categories.FirstOrDefaultAsync(
            c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(GuidedCategory), request.CategoryId);

        var before = JsonSerializer.Serialize(new { category.IsDeleted, category.IsActive });

        category.IsDeleted = true;
        category.IsActive  = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        _categories.Update(category);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "DELETE_CATEGORY",
            EntityType  = "guided_categories",
            EntityId    = category.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsDeleted = true, IsActive = false }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation("DeleteCategory: category {CategoryId} soft-deleted", category.Id);
    }
}
