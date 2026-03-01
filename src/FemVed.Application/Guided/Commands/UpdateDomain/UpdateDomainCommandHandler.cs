using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.UpdateDomain;

/// <summary>
/// Handles <see cref="UpdateDomainCommand"/>.
/// Applies partial updates to a guided domain and writes an audit log entry.
/// </summary>
public sealed class UpdateDomainCommandHandler : IRequestHandler<UpdateDomainCommand>
{
    private readonly IRepository<GuidedDomain> _domains;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateDomainCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateDomainCommandHandler(
        IRepository<GuidedDomain> domains,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<UpdateDomainCommandHandler> logger)
    {
        _domains   = domains;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Applies partial updates to the domain and logs the change.</summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the domain does not exist or is already deleted.</exception>
    /// <exception cref="DomainException">Thrown when the requested slug is already taken by another domain.</exception>
    public async Task Handle(UpdateDomainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateDomain: admin {AdminId} updating domain {DomainId}",
            request.AdminUserId, request.DomainId);

        var domain = await _domains.FirstOrDefaultAsync(
            d => d.Id == request.DomainId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(GuidedDomain), request.DomainId);

        // Validate slug uniqueness if slug is being changed
        if (request.Slug is not null && request.Slug != domain.Slug)
        {
            var slugTaken = await _domains.AnyAsync(
                d => d.Slug == request.Slug && d.Id != request.DomainId && !d.IsDeleted,
                cancellationToken);
            if (slugTaken)
                throw new DomainException($"A domain with slug '{request.Slug}' already exists.");
        }

        var before = JsonSerializer.Serialize(new { domain.Name, domain.Slug, domain.SortOrder });

        if (request.Name is not null)     domain.Name      = request.Name.Trim();
        if (request.Slug is not null)     domain.Slug      = request.Slug.Trim().ToLowerInvariant();
        if (request.SortOrder is not null) domain.SortOrder = request.SortOrder.Value;
        domain.UpdatedAt = DateTimeOffset.UtcNow;
        _domains.Update(domain);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "UPDATE_DOMAIN",
            EntityType  = "guided_domains",
            EntityId    = domain.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { domain.Name, domain.Slug, domain.SortOrder }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("UpdateDomain: domain {DomainId} updated", domain.Id);
    }
}
