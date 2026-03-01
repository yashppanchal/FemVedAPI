using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Guided.Commands.DeleteDomain;

/// <summary>
/// Handles <see cref="DeleteDomainCommand"/>.
/// Sets IsDeleted = true, IsActive = false on the domain, and writes an audit log entry.
/// </summary>
public sealed class DeleteDomainCommandHandler : IRequestHandler<DeleteDomainCommand>
{
    private readonly IRepository<GuidedDomain> _domains;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeleteDomainCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeleteDomainCommandHandler(
        IRepository<GuidedDomain> domains,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<DeleteDomainCommandHandler> logger)
    {
        _domains   = domains;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Soft-deletes the domain and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the domain does not exist or is already deleted.</exception>
    public async Task Handle(DeleteDomainCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteDomain: admin {AdminId} soft-deleting domain {DomainId}",
            request.AdminUserId, request.DomainId);

        var domain = await _domains.FirstOrDefaultAsync(
            d => d.Id == request.DomainId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(GuidedDomain), request.DomainId);

        var before = JsonSerializer.Serialize(new { domain.IsDeleted, domain.IsActive });

        domain.IsDeleted  = true;
        domain.IsActive   = false;
        domain.UpdatedAt  = DateTimeOffset.UtcNow;
        _domains.Update(domain);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "DELETE_DOMAIN",
            EntityType  = "guided_domains",
            EntityId    = domain.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsDeleted = true, IsActive = false }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DeleteDomain: domain {DomainId} soft-deleted", domain.Id);
    }
}
