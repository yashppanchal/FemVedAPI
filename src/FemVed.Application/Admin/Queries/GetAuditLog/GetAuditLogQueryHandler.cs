using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAuditLog;

/// <summary>Handles <see cref="GetAuditLogQuery"/>. Returns recent audit entries with admin email resolved.</summary>
public sealed class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, List<AuditLogDto>>
{
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IRepository<User> _users;
    private readonly ILogger<GetAuditLogQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAuditLogQueryHandler(
        IRepository<AdminAuditLog> auditLogs,
        IRepository<User> users,
        ILogger<GetAuditLogQueryHandler> logger)
    {
        _auditLogs = auditLogs;
        _users     = users;
        _logger    = logger;
    }

    /// <summary>Returns the most recent audit log entries ordered by timestamp descending.</summary>
    public async Task<List<AuditLogDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAuditLog: loading last {Limit} entries", request.Limit);

        var entries = await _auditLogs.GetAllAsync(cancellationToken: cancellationToken);

        var adminIds = entries.Select(e => e.AdminUserId).Distinct().ToHashSet();
        var admins   = await _users.GetAllAsync(u => adminIds.Contains(u.Id), cancellationToken);
        var adminMap = admins.ToDictionary(u => u.Id);

        var result = entries
            .OrderByDescending(e => e.CreatedAt)
            .Take(request.Limit)
            .Select(e =>
            {
                adminMap.TryGetValue(e.AdminUserId, out var admin);
                return new AuditLogDto(
                    LogId:        e.Id,
                    AdminUserId:  e.AdminUserId,
                    AdminEmail:   admin?.Email ?? string.Empty,
                    Action:       e.Action,
                    EntityType:   e.EntityType,
                    EntityId:     e.EntityId,
                    BeforeValue:  e.BeforeValue,
                    AfterValue:   e.AfterValue,
                    IpAddress:    e.IpAddress,
                    CreatedAt:    e.CreatedAt);
            })
            .ToList();

        _logger.LogInformation("GetAuditLog: returned {Count} entries", result.Count);
        return result;
    }
}
