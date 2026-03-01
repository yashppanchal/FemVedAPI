using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.CreateExpert;

/// <summary>
/// Handles <see cref="CreateExpertCommand"/>.
/// Creates an expert profile for an existing user account and writes an audit log entry.
/// Throws if the user does not exist or already has an active expert profile.
/// </summary>
public sealed class CreateExpertCommandHandler : IRequestHandler<CreateExpertCommand, Guid>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<User> _users;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateExpertCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public CreateExpertCommandHandler(
        IRepository<Expert> experts,
        IRepository<User> users,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<CreateExpertCommandHandler> logger)
    {
        _experts   = experts;
        _users     = users;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Creates the expert profile and logs the action.</summary>
    /// <param name="request">The create command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new expert profile ID.</returns>
    /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the user already has an active expert profile.</exception>
    public async Task<Guid> Handle(CreateExpertCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("CreateExpert: admin {AdminId} creating expert for user {UserId}",
            request.AdminUserId, request.UserId);

        // Verify the user account exists
        var userExists = await _users.AnyAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        if (!userExists)
            throw new NotFoundException(nameof(User), request.UserId);

        // Prevent duplicate expert profiles for the same user
        var alreadyExists = await _experts.AnyAsync(
            e => e.UserId == request.UserId && !e.IsDeleted, cancellationToken);
        if (alreadyExists)
            throw new DomainException($"User {request.UserId} already has an active expert profile.");

        var expert = new Expert
        {
            Id                  = Guid.NewGuid(),
            UserId              = request.UserId,
            DisplayName         = request.DisplayName.Trim(),
            Title               = request.Title.Trim(),
            Bio                 = request.Bio.Trim(),
            GridDescription     = request.GridDescription?.Trim(),
            DetailedDescription = request.DetailedDescription?.Trim(),
            ProfileImageUrl     = request.ProfileImageUrl?.Trim(),
            GridImageUrl        = request.GridImageUrl?.Trim(),
            Specialisations     = request.Specialisations?.Select(s => s.Trim()).ToArray(),
            YearsExperience     = request.YearsExperience,
            Credentials         = request.Credentials?.Select(c => c.Trim()).ToArray(),
            LocationCountry     = request.LocationCountry?.Trim(),
            IsActive            = true,
            IsDeleted           = false,
            CreatedAt           = DateTimeOffset.UtcNow,
            UpdatedAt           = DateTimeOffset.UtcNow
        };

        await _experts.AddAsync(expert);

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = "CREATE_EXPERT",
            EntityType  = "experts",
            EntityId    = expert.Id,
            BeforeValue = null,
            AfterValue  = JsonSerializer.Serialize(new
            {
                expert.UserId,
                expert.DisplayName,
                expert.Title,
                expert.LocationCountry
            }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CreateExpert: expert {ExpertId} created for user {UserId}",
            expert.Id, expert.UserId);

        return expert.Id;
    }
}
