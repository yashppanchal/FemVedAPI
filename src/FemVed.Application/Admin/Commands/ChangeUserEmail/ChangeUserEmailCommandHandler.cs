using System.Text.Json;
using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.ChangeUserEmail;

/// <summary>
/// Handles <see cref="ChangeUserEmailCommand"/>.
/// Updates the user's email address and writes an audit log entry.
/// </summary>
public sealed class ChangeUserEmailCommandHandler : IRequestHandler<ChangeUserEmailCommand, AdminUserDto>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ChangeUserEmailCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public ChangeUserEmailCommandHandler(
        IRepository<User> users,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<ChangeUserEmailCommandHandler> logger)
    {
        _users     = users;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>
    /// Changes the user's email and logs the action.
    /// </summary>
    /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the new email is already in use.</exception>
    public async Task<AdminUserDto> Handle(ChangeUserEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ChangeUserEmail: admin {AdminId} changing email for user {UserId}",
            request.AdminUserId, request.TargetUserId);

        var user = await _users.FirstOrDefaultAsync(
            u => u.Id == request.TargetUserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.TargetUserId);

        var emailLower = request.NewEmail.Trim().ToLowerInvariant();

        var emailTaken = await _users.AnyAsync(
            u => u.Email.ToLower() == emailLower && u.Id != request.TargetUserId && !u.IsDeleted,
            cancellationToken);

        if (emailTaken)
            throw new DomainException($"Email '{request.NewEmail}' is already in use by another account.");

        var before = JsonSerializer.Serialize(new { user.Email });

        user.Email     = request.NewEmail.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        _users.Update(user);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "CHANGE_USER_EMAIL",
            EntityType  = "users",
            EntityId    = user.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new { Email = request.NewEmail.Trim() }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ChangeUserEmail: email updated for user {UserId}", user.Id);

        return new AdminUserDto(
            UserId:          user.Id,
            Email:           user.Email,
            FirstName:       user.FirstName,
            LastName:        user.LastName,
            RoleId:          user.RoleId,
            RoleName:        ((UserRole)user.RoleId).ToString(),
            CountryIsoCode:  user.CountryIsoCode,
            FullMobile:      user.FullMobile,
            IsEmailVerified: user.IsEmailVerified,
            WhatsAppOptIn:   user.WhatsAppOptIn,
            IsActive:        user.IsActive,
            IsDeleted:       user.IsDeleted,
            CreatedAt:       user.CreatedAt);
    }
}
