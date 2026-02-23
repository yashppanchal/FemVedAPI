using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.ActivateUser;

/// <summary>Handles <see cref="ActivateUserCommand"/>. Sets IsActive = true and writes an audit log entry.</summary>
public sealed class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ActivateUserCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public ActivateUserCommandHandler(
        IRepository<User> users,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<ActivateUserCommandHandler> logger)
    {
        _users     = users;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Activates the user and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
    public async Task Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ActivateUser: admin {AdminId} activating user {UserId}", request.AdminUserId, request.TargetUserId);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.TargetUserId);

        var before = JsonSerializer.Serialize(new { user.IsActive });
        user.IsActive  = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        _users.Update(user);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "ACTIVATE_USER",
            EntityType  = "users",
            EntityId    = user.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { IsActive = true }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ActivateUser: user {UserId} activated", user.Id);
    }
}
