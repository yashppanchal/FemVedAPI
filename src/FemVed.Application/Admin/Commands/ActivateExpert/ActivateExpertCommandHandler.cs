using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.ActivateExpert;

/// <summary>Handles <see cref="ActivateExpertCommand"/>. Sets IsActive = true and writes an audit log entry.</summary>
public sealed class ActivateExpertCommandHandler : IRequestHandler<ActivateExpertCommand>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ActivateExpertCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public ActivateExpertCommandHandler(
        IRepository<Expert> experts,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<ActivateExpertCommandHandler> logger)
    {
        _experts   = experts;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Activates the expert profile and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the expert does not exist.</exception>
    public async Task Handle(ActivateExpertCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ActivateExpert: admin {AdminId} activating expert {ExpertId}", request.AdminUserId, request.ExpertId);

        var expert = await _experts.FirstOrDefaultAsync(e => e.Id == request.ExpertId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Expert), request.ExpertId);

        var before = JsonSerializer.Serialize(new { expert.IsActive });
        expert.IsActive  = true;
        expert.UpdatedAt = DateTimeOffset.UtcNow;
        _experts.Update(expert);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "ACTIVATE_EXPERT",
            EntityType  = "experts",
            EntityId    = expert.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsActive = true }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ActivateExpert: expert {ExpertId} activated", expert.Id);
    }
}
