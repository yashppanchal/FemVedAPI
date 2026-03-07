using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace FemVed.Application.Guided.Commands.DeleteProgram;

/// <summary>
/// Handles <see cref="DeleteProgramCommand"/>.
/// Cascades the soft-delete to all ProgramDurations (IsActive=false).
/// Non-admin callers must own the program (Expert.UserId == request.UserId).
/// All changes are saved in a single transaction.
/// </summary>
public sealed class DeleteProgramCommandHandler : IRequestHandler<DeleteProgramCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Program> _programs;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramDuration> _durations;
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DeleteProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteProgramCommandHandler(
        IRepository<Program> programs,
        IRepository<Expert> experts,
        IRepository<ProgramDuration> durations,
        IRepository<UserProgramAccess> access,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<DeleteProgramCommandHandler> logger)
    {
        _programs  = programs;
        _experts   = experts;
        _durations = durations;
        _access    = access;
        _auditLogs = auditLogs;
        _uow       = uow;
        _cache     = cache;
        _logger    = logger;
    }

    /// <summary>Soft-deletes the program and cascades to all durations.</summary>
    /// <exception cref="NotFoundException">Thrown when the program does not exist or is already deleted.</exception>
    /// <exception cref="ForbiddenException">Thrown when a non-admin caller does not own the program.</exception>
    public async Task Handle(DeleteProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteProgram: user {UserId} (isAdmin={IsAdmin}) soft-deleting program {ProgramId}",
            request.UserId, request.IsAdmin, request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Program), request.ProgramId);

        // Non-admin: verify the caller's Expert record owns this program
        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.UserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("You do not have an expert profile.");

            if (expert.Id != program.ExpertId)
                throw new ForbiddenException("You can only delete your own programs.");
        }

        // ── Fix 8: guard against active enrollments ──────────────────────────
        var hasActiveEnrollments = await _access.AnyAsync(
            a => a.ProgramId == request.ProgramId
              && a.Status != UserProgramAccessStatus.Completed
              && a.Status != UserProgramAccessStatus.Cancelled,
            cancellationToken);

        if (hasActiveEnrollments)
            throw new DomainException(
                "Cannot delete a program that has active enrollments. " +
                "Complete or cancel all enrollments first.");

        var before = JsonSerializer.Serialize(new { program.IsDeleted, program.IsActive, program.Status });

        // ── 1. Mark program ──────────────────────────────────────────────────
        program.IsDeleted = true;
        program.IsActive  = false;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);

        // ── 2. Cascade to durations (no IsDeleted — deactivate only) ────────
        var durations = await _durations.GetAllAsync(
            d => d.ProgramId == request.ProgramId && d.IsActive, cancellationToken);

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
            AdminUserId = request.UserId,
            Action      = "DELETE_PROGRAM",
            EntityType  = "programs",
            EntityId    = program.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new
            {
                IsDeleted = true, IsActive = false,
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
            "DeleteProgram: program {ProgramId} soft-deleted with cascade — {Durations} durations deactivated",
            program.Id, durations.Count);
    }
}
