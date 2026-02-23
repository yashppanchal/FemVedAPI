using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.DeactivateUser;

/// <summary>Handles <see cref="DeactivateUserCommand"/>. Sets IsActive = false and writes an audit log entry.</summary>
public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeactivateUserCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public DeactivateUserCommandHandler(
        IRepository<User> users,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<DeactivateUserCommandHandler> logger)
    {
        _users     = users;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Deactivates the user and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DeactivateUser: admin {AdminId} deactivating user {UserId}", request.AdminUserId, request.TargetUserId);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.TargetUserId);

        var before = JsonSerializer.Serialize(new { user.IsActive });
        user.IsActive  = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        _users.Update(user);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "DEACTIVATE_USER",
            EntityType  = "users",
            EntityId    = user.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsActive = false }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DeactivateUser: user {UserId} deactivated", user.Id);
    }
}
