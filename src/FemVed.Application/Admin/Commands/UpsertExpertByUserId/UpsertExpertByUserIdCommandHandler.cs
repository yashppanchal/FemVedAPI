using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.UpsertExpertByUserId;

/// <summary>
/// Handles <see cref="UpsertExpertByUserIdCommand"/>.
/// If an expert profile already exists for the user, applies a partial update.
/// If none exists (edge case — normally auto-created by ChangeUserRoleCommandHandler),
/// creates a new profile with the supplied fields.
/// Writes an audit log entry in both cases.
/// </summary>
public sealed class UpsertExpertByUserIdCommandHandler : IRequestHandler<UpsertExpertByUserIdCommand>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<User> _users;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpsertExpertByUserIdCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpsertExpertByUserIdCommandHandler(
        IRepository<Expert> experts,
        IRepository<User> users,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<UpsertExpertByUserIdCommandHandler> logger)
    {
        _experts   = experts;
        _users     = users;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>
    /// Creates or updates the expert profile and logs the action.
    /// </summary>
    /// <param name="request">The upsert command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the user does not exist.</exception>
    public async Task Handle(UpsertExpertByUserIdCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "UpsertExpertByUserId: admin {AdminId} upserting expert profile for user {UserId}",
            request.AdminUserId, request.UserId);

        // Verify the user account exists
        var userExists = await _users.AnyAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        if (!userExists)
            throw new NotFoundException(nameof(User), request.UserId);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.UserId == request.UserId && !e.IsDeleted, cancellationToken);

        if (expert is not null)
        {
            // ── Update path ──────────────────────────────────────────────────────
            var before = JsonSerializer.Serialize(new
            {
                expert.DisplayName,
                expert.Title,
                expert.Bio,
                expert.LocationCountry,
                expert.YearsExperience,
                expert.CommissionRate
            });

            if (request.DisplayName is not null)         expert.DisplayName         = request.DisplayName.Trim();
            if (request.Title is not null)               expert.Title               = request.Title.Trim();
            if (request.Bio is not null)                 expert.Bio                 = request.Bio.Trim();
            if (request.GridDescription is not null)     expert.GridDescription     = request.GridDescription.Trim();
            if (request.DetailedDescription is not null) expert.DetailedDescription = request.DetailedDescription.Trim();
            if (request.ProfileImageUrl is not null)     expert.ProfileImageUrl     = request.ProfileImageUrl.Trim();
            if (request.GridImageUrl is not null)        expert.GridImageUrl        = request.GridImageUrl.Trim();
            if (request.Specialisations is not null)     expert.Specialisations     = request.Specialisations.Select(s => s.Trim()).ToArray();
            if (request.YearsExperience is not null)     expert.YearsExperience     = request.YearsExperience;
            if (request.Credentials is not null)         expert.Credentials         = request.Credentials.Select(c => c.Trim()).ToArray();
            if (request.LocationCountry is not null)     expert.LocationCountry     = request.LocationCountry.Trim();
            expert.UpdatedAt = DateTimeOffset.UtcNow;
            _experts.Update(expert);

            await _auditLogs.AddAsync(new AdminAuditLog
            {
                Id          = Guid.NewGuid(),
                AdminUserId = request.AdminUserId,
                Action      = "UPSERT_EXPERT_PROFILE",
                EntityType  = "experts",
                EntityId    = expert.Id,
                BeforeValue = before,
                AfterValue  = JsonSerializer.Serialize(new
                {
                    expert.DisplayName,
                    expert.Title,
                    expert.Bio,
                    expert.LocationCountry,
                    expert.YearsExperience,
                    expert.CommissionRate
                }),
                IpAddress   = request.IpAddress,
                CreatedAt   = DateTimeOffset.UtcNow
            });

            _logger.LogInformation(
                "UpsertExpertByUserId: expert {ExpertId} (userId={UserId}) profile updated",
                expert.Id, request.UserId);
        }
        else
        {
            // ── Create path (edge case — role change handler normally creates this) ─
            var user = await _users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken)!;
            var fallbackName = user is not null
                ? $"{user.FirstName} {user.LastName}".Trim()
                : string.Empty;

            var newExpert = new Expert
            {
                Id                  = Guid.NewGuid(),
                UserId              = request.UserId,
                DisplayName         = !string.IsNullOrWhiteSpace(request.DisplayName) ? request.DisplayName.Trim() : fallbackName,
                Title               = request.Title?.Trim() ?? string.Empty,
                Bio                 = request.Bio?.Trim() ?? string.Empty,
                GridDescription     = request.GridDescription?.Trim(),
                DetailedDescription = request.DetailedDescription?.Trim(),
                ProfileImageUrl     = request.ProfileImageUrl?.Trim(),
                GridImageUrl        = request.GridImageUrl?.Trim(),
                Specialisations     = request.Specialisations?.Select(s => s.Trim()).ToArray(),
                YearsExperience     = request.YearsExperience,
                Credentials         = request.Credentials?.Select(c => c.Trim()).ToArray(),
                LocationCountry     = request.LocationCountry?.Trim(),
                CommissionRate      = 80.00m,
                IsActive            = true,
                IsDeleted           = false,
                CreatedAt           = DateTimeOffset.UtcNow,
                UpdatedAt           = DateTimeOffset.UtcNow
            };

            await _experts.AddAsync(newExpert);

            await _auditLogs.AddAsync(new AdminAuditLog
            {
                Id          = Guid.NewGuid(),
                AdminUserId = request.AdminUserId,
                Action      = "UPSERT_EXPERT_PROFILE",
                EntityType  = "experts",
                EntityId    = newExpert.Id,
                BeforeValue = null,
                AfterValue  = JsonSerializer.Serialize(new
                {
                    newExpert.UserId,
                    newExpert.DisplayName,
                    newExpert.Title,
                    newExpert.LocationCountry
                }),
                IpAddress   = request.IpAddress,
                CreatedAt   = DateTimeOffset.UtcNow
            });

            _logger.LogInformation(
                "UpsertExpertByUserId: expert {ExpertId} created (edge case) for user {UserId}",
                newExpert.Id, request.UserId);
        }

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
