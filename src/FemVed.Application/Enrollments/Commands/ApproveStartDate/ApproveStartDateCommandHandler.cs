using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Enrollments.Commands.ApproveStartDate;

/// <summary>
/// Handles <see cref="ApproveStartDateCommand"/>.
/// Approves the user's requested start date, schedules the enrollment,
/// and notifies the user by email.
/// </summary>
public sealed class ApproveStartDateCommandHandler : IRequestHandler<ApproveStartDateCommand>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<User> _users;
    private readonly IRepository<NotificationLog> _notifLogs;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ApproveStartDateCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public ApproveStartDateCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<User> users,
        IRepository<NotificationLog> notifLogs,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<ApproveStartDateCommandHandler> logger)
    {
        _access      = access;
        _experts     = experts;
        _users       = users;
        _notifLogs   = notifLogs;
        _emailService = emailService;
        _uow         = uow;
        _logger      = logger;
    }

    /// <summary>Approves the user's requested start date and schedules the enrollment.</summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller is not the expert for this program.</exception>
    /// <exception cref="DomainException">Thrown when there is no pending start date request.</exception>
    public async Task Handle(ApproveStartDateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "ApproveStartDate: user {UserId} (isAdmin={IsAdmin}) approving start date for access {AccessId}",
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

        if (record.StartRequestStatus != StartRequestStatus.Pending || record.RequestedStartDate is null)
            throw new DomainException("There is no pending start date request for this enrollment.");

        var now = DateTimeOffset.UtcNow;
        record.StartRequestStatus = StartRequestStatus.Approved;
        record.ScheduledStartAt   = record.RequestedStartDate;
        record.UpdatedAt          = now;
        _access.Update(record);

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ApproveStartDate: access {AccessId} approved — ScheduledStartAt={Date}",
            record.Id, record.ScheduledStartAt);

        // Notify user their request was approved
        await TrySendEmailAsync(record, request.PerformedByUserId, cancellationToken);
    }

    private async Task TrySendEmailAsync(
        UserProgramAccess record,
        Guid performedByUserId,
        CancellationToken ct)
    {
        try
        {
            var user = await _users.FirstOrDefaultAsync(u => u.Id == record.UserId, ct);
            if (user is null) return;

            var startLabel = record.ScheduledStartAt?.ToString("MMMM d, yyyy") ?? string.Empty;
            var templateData = new Dictionary<string, object>
            {
                ["first_name"] = user.FirstName,
                ["start_date"] = startLabel
            };

            await _emailService.SendAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                "start_date_approved",
                templateData,
                ct);

            await _notifLogs.AddAsync(new NotificationLog
            {
                Id          = Guid.NewGuid(),
                UserId      = user.Id,
                Type        = NotificationType.Email,
                TemplateKey = "start_date_approved",
                Recipient   = user.Email,
                Status      = NotificationStatus.Sent,
                Payload     = JsonSerializer.Serialize(templateData),
                CreatedAt   = DateTimeOffset.UtcNow
            });
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApproveStartDate: failed to send approval email for access {AccessId}", record.Id);
        }
    }
}
