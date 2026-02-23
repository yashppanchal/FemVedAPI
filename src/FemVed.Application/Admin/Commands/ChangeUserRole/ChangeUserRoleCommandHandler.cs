using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.ChangeUserRole;

/// <summary>Handles <see cref="ChangeUserRoleCommand"/>. Updates the user's role and writes an audit log entry.</summary>
public sealed class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ChangeUserRoleCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public ChangeUserRoleCommandHandler(
        IRepository<User> users,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<ChangeUserRoleCommandHandler> logger)
    {
        _users     = users;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Changes the user's role and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the user already has the requested role.</exception>
    public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ChangeUserRole: admin {AdminId} changing role of user {UserId} to {RoleId}",
            request.AdminUserId, request.TargetUserId, request.RoleId);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.TargetUserId);

        if (user.RoleId == request.RoleId)
            throw new DomainException($"User already has role {request.RoleId}.");

        var before = JsonSerializer.Serialize(new { user.RoleId });

        user.RoleId    = request.RoleId;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        _users.Update(user);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "CHANGE_USER_ROLE",
            EntityType  = "users",
            EntityId    = user.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { RoleId = request.RoleId }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ChangeUserRole: user {UserId} role changed to {RoleId}", user.Id, request.RoleId);
    }
}
