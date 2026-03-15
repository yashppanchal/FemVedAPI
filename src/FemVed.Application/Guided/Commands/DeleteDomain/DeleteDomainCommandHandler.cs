using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace FemVed.Application.Guided.Commands.DeleteDomain;

/// <summary>
/// Handles <see cref="DeleteDomainCommand"/>.
/// Cascades the soft-delete down the full hierarchy:
/// Domain → all Categories → all Programs → all ProgramDurations (IsActive=false).
/// All changes are saved in a single transaction.
/// </summary>
public sealed class DeleteDomainCommandHandler : IRequestHandler<DeleteDomainCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<GuidedDomain> _domains;
    private readonly IRepository<GuidedCategory> _categories;
    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteDomainCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteDomainCommandHandler(
        IRepository<GuidedDomain> domains,
        IRepository<GuidedCategory> categories,
        IRepository<Domain.Entities.Program> programs,
        IRepository<ProgramDuration> durations,
        IRepository<UserProgramAccess> access,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteDomainCommandHandler> logger)
    {
        _domains    = domains;
        _categories = categories;
        _programs   = programs;
        _durations  = durations;
        _access     = access;
        _auditLogs  = auditLogs;
        _uow        = uow;
        _cache      = cache;
        _logger     = logger;
    }

    /// <summary>Soft-deletes the domain and cascades to all child entities.</summary>
    /// <exception cref="NotFoundException">Thrown when the domain does not exist or is already deleted.</exception>
    public async Task Handle(DeleteDomainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteDomain: admin {AdminId} soft-deleting domain {DomainId}",
            request.AdminUserId, request.DomainId);

        var domain = await _domains.FirstOrDefaultAsync(
            d => d.Id == request.DomainId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(GuidedDomain), request.DomainId);

        var before = JsonSerializer.Serialize(new { domain.IsDeleted, domain.IsActive });

        // ── 1. Mark domain ───────────────────────────────────────────────────
        domain.IsDeleted = true;
        domain.IsActive  = false;
        domain.UpdatedAt = DateTimeOffset.UtcNow;
        _domains.Update(domain);

        // ── 2. Cascade to categories ─────────────────────────────────────────
        var categories = await _categories.GetAllAsync(
            c => c.DomainId == request.DomainId && !c.IsDeleted, cancellationToken);

        var categoryIds = new HashSet<Guid>(categories.Select(c => c.Id));

        foreach (var cat in categories)
        {
            cat.IsDeleted = true;
            cat.IsActive  = false;
            cat.UpdatedAt = DateTimeOffset.UtcNow;
            _categories.Update(cat);
        }

        // ── 3. Cascade to programs ───────────────────────────────────────────
        var programs = categoryIds.Count > 0
            ? await _programs.GetAllAsync(
                p => categoryIds.Contains(p.CategoryId) && !p.IsDeleted, cancellationToken)
            : [];

        var programIds = new HashSet<Guid>(programs.Select(p => p.Id));

        // ── 3a. Guard: block if any program has active enrollments ───────────
        if (programIds.Count > 0)
        {
            var hasActive = await _access.AnyAsync(
                a => programIds.Contains(a.ProgramId) &&
                     a.Status != UserProgramAccessStatus.Completed &&
                     a.Status != UserProgramAccessStatus.Cancelled,
                cancellationToken);

            if (hasActive)
                throw new DomainException(
                    "Cannot archive this domain — one or more of its programs have active enrollments.");
        }

        foreach (var prog in programs)
        {
            prog.IsDeleted = true;
            prog.IsActive  = false;
            prog.UpdatedAt = DateTimeOffset.UtcNow;
            _programs.Update(prog);
        }

        // ── 4. Cascade to durations (no IsDeleted — deactivate only) ────────
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
            Action      = "DELETE_DOMAIN",
            EntityType  = "guided_domains",
            EntityId    = domain.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new
            {
                IsDeleted = true, IsActive = false,
                CascadedCategories = categories.Count,
                CascadedPrograms   = programs.Count,
                CascadedDurations  = durations.Count
            }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        // ── Single transaction save ──────────────────────────────────────────
        await _uow.SaveChangesAsync(cancellationToken);

        foreach (var loc in KnownLocationCodes)
            _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");

        _logger.LogInformation(
            "DeleteDomain: domain {DomainId} soft-deleted with cascade — {Categories} categories, {Programs} programs, {Durations} durations",
            domain.Id, categories.Count, programs.Count, durations.Count);
    }
}
