using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace FemVed.Application.Guided.Commands.DeleteCategory;

/// <summary>
/// Handles <see cref="DeleteCategoryCommand"/>.
/// Cascades the soft-delete down the hierarchy:
/// Category → all Programs → all ProgramDurations (IsActive=false).
/// All changes are saved in a single transaction.
/// </summary>
public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<GuidedCategory> _categories;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteCategoryCommandHandler(
        IRepository<GuidedCategory> categories,
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteCategoryCommandHandler> logger)
    {
        _categories = categories;
        _programs   = programs;
        _durations  = durations;
        _auditLogs  = auditLogs;
        _uow        = uow;
        _cache      = cache;
        _logger     = logger;
    }

    /// <summary>Soft-deletes the category and cascades to all child entities.</summary>
    /// <exception cref="NotFoundException">Thrown when the category does not exist or is already deleted.</exception>
    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteCategory: admin {AdminId} soft-deleting category {CategoryId}",
            request.AdminUserId, request.CategoryId);

        var category = await _categories.FirstOrDefaultAsync(
            c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(GuidedCategory), request.CategoryId);

        var before = JsonSerializer.Serialize(new { category.IsDeleted, category.IsActive });

        // ── 1. Mark category ─────────────────────────────────────────────────
        category.IsDeleted = true;
        category.IsActive  = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        _categories.Update(category);

        // ── 2. Cascade to programs ───────────────────────────────────────────
        var programs = await _programs.GetAllAsync(
            p => p.CategoryId == request.CategoryId && !p.IsDeleted, cancellationToken);

        var programIds = new HashSet<Guid>(programs.Select(p => p.Id));

        foreach (var prog in programs)
        {
            prog.IsDeleted = true;
            prog.IsActive  = false;
            prog.UpdatedAt = DateTimeOffset.UtcNow;
            _programs.Update(prog);
        }

        // ── 3. Cascade to durations ──────────────────────────────────────────
        var durations = programIds.Count > 0
            ? await _durations.GetAllAsync(
                d => programIds.Contains(d.ProgramId) && d.IsActive, cancellationToken)
            : [];

        foreach (var dur in durations)
        {
            dur.IsActive  = false;
            dur.UpdatedAt = DateTimeOffset.UtcNow;
            _durations.Update(dur);
        }

        // ── Audit log ────────────────────────────────────────────────────────
        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "DELETE_CATEGORY",
            EntityType  = "guided_categories",
            EntityId    = category.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new
            {
                IsDeleted = true, IsActive = false,
                CascadedPrograms  = programs.Count,
                CascadedDurations = durations.Count
            }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        // ── Single transaction save ──────────────────────────────────────────
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "DeleteCategory: category {CategoryId} soft-deleted with cascade — {Programs} programs, {Durations} durations",
            category.Id, programs.Count, durations.Count);
    }
}
