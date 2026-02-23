using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.DeleteUser;

/// <summary>Handles <see cref="DeleteUserCommand"/>. Sets IsDeleted = true (never hard-deletes) and writes an audit log entry.</summary>
public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeleteUserCommandHandler(
        IRepository<User> users,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _users     = users;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Soft-deletes the user and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the user does not exist or is already deleted.</exception>
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeleteUser: admin {AdminId} soft-deleting user {UserId}", request.AdminUserId, request.TargetUserId);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.TargetUserId);

        var before = JsonSerializer.Serialize(new { user.IsDeleted, user.IsActive });
        user.IsDeleted = true;
        user.IsActive  = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        _users.Update(user);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "DELETE_USER",
            EntityType  = "users",
            EntityId    = user.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsDeleted = true, IsActive = false }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DeleteUser: user {UserId} soft-deleted", user.Id);
    }
}
