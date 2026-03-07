using System.Text.Json;
using FemVed.Application.Guided.Queries.GetGuidedTree;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.UpdateProgram;

/// <summary>
/// Handles <see cref="UpdateProgramCommand"/>.
/// Applies partial updates. Experts can only edit programs they own in DRAFT or PENDING_REVIEW status.
/// List fields (WhatYouGet, WhoIsThisFor, Tags, DetailSections) are replaced wholesale when non-null.
/// All mutations (both admin and expert) are recorded in admin_audit_log.
/// </summary>
public sealed class UpdateProgramCommandHandler : IRequestHandler<UpdateProgramCommand>
{
    private static readonly string[] KnownLocationCodes =
        ["IN", "GB", "US", "AU", "AE", "NZ", "IE", "DE", "FR", "NL", "SG", "MY", "ZA", "LK"];

    private readonly IRepository<Domain.Entities.Program> _programs;
    private readonly IRepository<Domain.Entities.Expert> _experts;
    private readonly IRepository<ProgramDetailSection> _detailSections;
    private readonly IRepository<ProgramWhatYouGet> _whatYouGet;
    private readonly IRepository<ProgramWhoIsThisFor> _whoIsThisFor;
    private readonly IRepository<ProgramTag> _tags;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UpdateProgramCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateProgramCommandHandler(
        IRepository<Domain.Entities.Program> programs,
        IRepository<Domain.Entities.Expert> experts,
        IRepository<ProgramDetailSection> detailSections,
        IRepository<ProgramWhatYouGet> whatYouGet,
        IRepository<ProgramWhoIsThisFor> whoIsThisFor,
        IRepository<ProgramTag> tags,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<UpdateProgramCommandHandler> logger)
    {
        _programs       = programs;
        _experts        = experts;
        _detailSections = detailSections;
        _whatYouGet     = whatYouGet;
        _whoIsThisFor   = whoIsThisFor;
        _tags           = tags;
        _auditLogs      = auditLogs;
        _uow            = uow;
        _cache          = cache;
        _logger         = logger;
    }

    /// <summary>Applies partial updates to the program.</summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the program is not found.</exception>
    /// <exception cref="ForbiddenException">Thrown when an Expert tries to edit another expert's program.</exception>
    /// <exception cref="DomainException">Thrown when a PUBLISHED or ARCHIVED program is updated by a non-Admin.</exception>
    public async Task Handle(UpdateProgramCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating program {ProgramId}", request.ProgramId);

        var program = await _programs.FirstOrDefaultAsync(
            p => p.Id == request.ProgramId && !p.IsDeleted,
            cancellationToken)
            ?? throw new NotFoundException("Program", request.ProgramId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.RequestingUserId && !e.IsDeleted,
                cancellationToken)
                ?? throw new ForbiddenException("Expert profile not found.");

            if (program.ExpertId != expert.Id)
                throw new ForbiddenException("You can only edit your own programs.");

            if (program.Status is ProgramStatus.Published or ProgramStatus.Archived)
                throw new DomainException("Published or archived programs cannot be edited.");
        }

        if (request.Name is not null) program.Name = request.Name.Trim();
        if (request.GridDescription is not null) program.GridDescription = request.GridDescription.Trim();
        if (request.GridImageUrl is not null) program.GridImageUrl = request.GridImageUrl.Trim();
        if (request.Overview is not null) program.Overview = request.Overview.Trim();
        if (request.SortOrder is not null) program.SortOrder = request.SortOrder.Value;
        program.UpdatedAt = DateTimeOffset.UtcNow;
        _programs.Update(program);

        // ── WhatYouGet: replace all when provided ───────────────────────────
        if (request.WhatYouGet is not null)
        {
            foreach (var old in await _whatYouGet.GetAllAsync(
                w => w.ProgramId == request.ProgramId, cancellationToken))
                _whatYouGet.Remove(old);

            for (var i = 0; i < request.WhatYouGet.Count; i++)
                await _whatYouGet.AddAsync(new ProgramWhatYouGet
                {
                    Id = Guid.NewGuid(), ProgramId = request.ProgramId,
                    ItemText = request.WhatYouGet[i].Trim(), SortOrder = i
                });
        }

        // ── WhoIsThisFor: replace all when provided ──────────────────────────
        if (request.WhoIsThisFor is not null)
        {
            foreach (var old in await _whoIsThisFor.GetAllAsync(
                w => w.ProgramId == request.ProgramId, cancellationToken))
                _whoIsThisFor.Remove(old);

            for (var i = 0; i < request.WhoIsThisFor.Count; i++)
                await _whoIsThisFor.AddAsync(new ProgramWhoIsThisFor
                {
                    Id = Guid.NewGuid(), ProgramId = request.ProgramId,
                    ItemText = request.WhoIsThisFor[i].Trim(), SortOrder = i
                });
        }

        // ── Tags: replace all when provided ─────────────────────────────────
        if (request.Tags is not null)
        {
            foreach (var old in await _tags.GetAllAsync(
                t => t.ProgramId == request.ProgramId, cancellationToken))
                _tags.Remove(old);

            foreach (var tag in request.Tags)
                await _tags.AddAsync(new ProgramTag
                {
                    Id = Guid.NewGuid(), ProgramId = request.ProgramId,
                    Tag = tag.Trim().ToLowerInvariant()
                });
        }

        // ── DetailSections: replace all when provided ────────────────────────
        if (request.DetailSections is not null)
        {
            foreach (var old in await _detailSections.GetAllAsync(
                s => s.ProgramId == request.ProgramId, cancellationToken))
                _detailSections.Remove(old);

            foreach (var section in request.DetailSections)
                await _detailSections.AddAsync(new ProgramDetailSection
                {
                    Id = Guid.NewGuid(), ProgramId = request.ProgramId,
                    Heading = section.Heading.Trim(),
                    Description = section.Description.Trim(),
                    SortOrder = section.SortOrder
                });
        }

        // ── Audit log ─────────────────────────────────────────────────────────
        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.RequestingUserId,
            Action      = "UPDATE_PROGRAM",
            EntityType  = "programs",
            EntityId    = program.Id,
            BeforeValue = null,   // full before snapshot would require re-loading all child lists; caller diff is sufficient
            AfterValue  = JsonSerializer.Serialize(new
            {
                request.Name,
                request.GridDescription,
                request.Overview,
                request.SortOrder,
                UpdatedBy = request.IsAdmin ? "ADMIN" : "EXPERT"
            }),
            IpAddress = null,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);

        if (program.Status == ProgramStatus.Published)
        {
            foreach (var loc in KnownLocationCodes)
                _cache.Remove($"{GetGuidedTreeQueryHandler.CacheKeyPrefix}{loc}");
        }

        _logger.LogInformation("Program {ProgramId} updated successfully", request.ProgramId);
    }
}
