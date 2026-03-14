using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Enrollments.Commands.DeclineStartDate;

/// <summary>
/// Handles <see cref="DeclineStartDateCommand"/>.
/// Declines the user's requested start date and notifies the user by email.
/// </summary>
public sealed class DeclineStartDateCommandHandler : IRequestHandler<DeclineStartDateCommand>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<User> _users;
    private readonly IRepository<NotificationLog> _notifLogs;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DeclineStartDateCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public DeclineStartDateCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<User> users,
        IRepository<NotificationLog> notifLogs,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<DeclineStartDateCommandHandler> logger)
    {
        _access       = access;
        _experts      = experts;
        _users        = users;
        _notifLogs    = notifLogs;
        _emailService = emailService;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Declines the user's requested start date.</summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller is not the expert for this program.</exception>
    /// <exception cref="DomainException">Thrown when there is no pending start date request.</exception>
    public async Task Handle(DeclineStartDateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "DeclineStartDate: user {UserId} (isAdmin={IsAdmin}) declining start date for access {AccessId}",
            request.PerformedByUserId, request.IsAdmin, request.AccessId);

        var record = await _access.FirstOrDefaultAsync(a => a.Id == request.AccessId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        if (!request.IsAdmin)
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.PerformedByUserId && !e.IsDeleted && e.IsActive, cancellationToken)
                ?? throw new ForbiddenException("You do not have an active expert profile.");
            if (expert.Id != record.ExpertId)
                throw new ForbiddenException("You can only manage enrollments for your own programs.");
        }

        if (record.StartRequestStatus != StartRequestStatus.Pending)
            throw new DomainException("There is no pending start date request for this enrollment.");

        var now = DateTimeOffset.UtcNow;
        record.StartRequestStatus = StartRequestStatus.Declined;
        record.RequestedStartDate = null;
        record.UpdatedAt          = now;
        _access.Update(record);

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("DeclineStartDate: access {AccessId} start date request declined", record.Id);

        // Notify user their request was declined
        await TrySendEmailAsync(record, cancellationToken);
    }

    private async Task TrySendEmailAsync(UserProgramAccess record, CancellationToken ct)
    {
        try
        {
            var user = await _users.FirstOrDefaultAsync(u => u.Id == record.UserId, ct);
            if (user is null) return;

            var templateData = new Dictionary<string, object>
            {
                ["firstName"] = user.FirstName,
                ["year"]      = DateTimeOffset.UtcNow.Year.ToString()
            };

            await _emailService.SendAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "start_date_declined",
                templateData,
                ct);

            await _notifLogs.AddAsync(new NotificationLog
            {
                Id          = Guid.NewGuid(),
                UserId      = user.Id,
                Type        = NotificationType.Email,
                TemplateKey = "start_date_declined",
                Recipient   = user.Email,
                Status      = NotificationStatus.Sent,
                Payload     = JsonSerializer.Serialize(templateData),
                CreatedAt   = DateTimeOffset.UtcNow
            });
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeclineStartDate: failed to send decline email for access {AccessId}", record.Id);
        }
    }
}
