using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.DeleteExpert;

/// <summary>Handles <see cref="DeleteExpertCommand"/>. Sets IsDeleted = true and IsActive = false (never hard-deletes) and writes an audit log entry.</summary>
public sealed class DeleteExpertCommandHandler : IRequestHandler<DeleteExpertCommand>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeleteExpertCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeleteExpertCommandHandler(
        IRepository<Expert> experts,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<DeleteExpertCommandHandler> logger)
    {
        _experts   = experts;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Soft-deletes the expert profile and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the expert does not exist or is already deleted.</exception>
    public async Task Handle(DeleteExpertCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteExpert: admin {AdminId} soft-deleting expert {ExpertId}", request.AdminUserId, request.ExpertId);

        var expert = await _experts.FirstOrDefaultAsync(e => e.Id == request.ExpertId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Expert), request.ExpertId);

        var before = JsonSerializer.Serialize(new { expert.IsDeleted, expert.IsActive });
        expert.IsDeleted = true;
        expert.IsActive  = false;
        expert.UpdatedAt = DateTimeOffset.UtcNow;
        _experts.Update(expert);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "DELETE_EXPERT",
            EntityType  = "experts",
            EntityId    = expert.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsDeleted = true, IsActive = false }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DeleteExpert: expert {ExpertId} soft-deleted", expert.Id);
    }
}
