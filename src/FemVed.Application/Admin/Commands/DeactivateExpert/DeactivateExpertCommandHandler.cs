using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.DeactivateExpert;

/// <summary>Handles <see cref="DeactivateExpertCommand"/>. Sets IsActive = false and writes an audit log entry.</summary>
public sealed class DeactivateExpertCommandHandler : IRequestHandler<DeactivateExpertCommand>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeactivateExpertCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeactivateExpertCommandHandler(
        IRepository<Expert> experts,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<DeactivateExpertCommandHandler> logger)
    {
        _experts   = experts;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Deactivates the expert profile and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the expert does not exist.</exception>
    public async Task Handle(DeactivateExpertCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeactivateExpert: admin {AdminId} deactivating expert {ExpertId}", request.AdminUserId, request.ExpertId);

        var expert = await _experts.FirstOrDefaultAsync(e => e.Id == request.ExpertId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Expert), request.ExpertId);

        var before = JsonSerializer.Serialize(new { expert.IsActive });
        expert.IsActive  = false;
        expert.UpdatedAt = DateTimeOffset.UtcNow;
        _experts.Update(expert);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "DEACTIVATE_EXPERT",
            EntityType  = "experts",
            EntityId    = expert.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsActive = false }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DeactivateExpert: expert {ExpertId} deactivated", expert.Id);
    }
}
