using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.UpdateExpert;

/// <summary>
/// Handles <see cref="UpdateExpertCommand"/>.
/// Applies partial updates to an expert profile and writes an audit log entry.
/// </summary>
public sealed class UpdateExpertCommandHandler : IRequestHandler<UpdateExpertCommand>
{
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UpdateExpertCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public UpdateExpertCommandHandler(
        IRepository<Expert> experts,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<UpdateExpertCommandHandler> logger)
    {
        _experts   = experts;
        _auditLogs = auditLogs;
        _uow       = uow;
        _logger    = logger;
    }

    /// <summary>Applies partial updates to the expert profile and logs the change.</summary>
    /// <param name="request">The update command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the expert profile does not exist or is deleted.</exception>
    public async Task Handle(UpdateExpertCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("UpdateExpert: admin {AdminId} updating expert {ExpertId}",
            request.AdminUserId, request.ExpertId);

        var expert = await _experts.FirstOrDefaultAsync(
            e => e.Id == request.ExpertId && !e.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Expert), request.ExpertId);

        var before = JsonSerializer.Serialize(new
        {
            expert.DisplayName,
            expert.Title,
            expert.Bio,
            expert.LocationCountry,
            expert.YearsExperience
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
            Action      = "UPDATE_EXPERT",
            EntityType  = "experts",
            EntityId    = expert.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new
            {
                expert.DisplayName,
                expert.Title,
                expert.Bio,
                expert.LocationCountry,
                expert.YearsExperience
            }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("UpdateExpert: expert {ExpertId} updated", expert.Id);
    }
}
