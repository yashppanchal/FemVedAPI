using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Enrollments.Commands.PauseEnrollment;

/// <summary>
/// Handles <see cref="PauseEnrollmentCommand"/>.
/// <list type="number">
///   <item>Verifies the enrollment exists and the caller is authorised to pause it.</item>
///   <item>Guards against invalid state transitions (must be ACTIVE).</item>
///   <item>Sets Status = PAUSED, PausedAt = now.</item>
///   <item>Appends a <see cref="ProgramSessionLog"/> entry.</item>
///   <item>Emails the enrolled user via the <c>session_paused</c> SendGrid template.</item>
/// </list>
/// </summary>
public sealed class PauseEnrollmentCommandHandler : IRequestHandler<PauseEnrollmentCommand>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramSessionLog> _sessionLogs;
    private readonly IRepository<User> _users;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PauseEnrollmentCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public PauseEnrollmentCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<ProgramSessionLog> sessionLogs,
        IRepository<User> users,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<PauseEnrollmentCommandHandler> logger)
    {
        _access       = access;
        _experts      = experts;
        _sessionLogs  = sessionLogs;
        _users        = users;
        _emailService = emailService;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Pauses the enrollment and notifies the enrolled user.</summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller is not authorised to pause this enrollment.</exception>
    /// <exception cref="DomainException">Thrown when the enrollment is not in ACTIVE status.</exception>
    public async Task Handle(PauseEnrollmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "PauseEnrollment: user {UserId} (isAdmin={IsAdmin}, isUser={IsUser}) pausing access {AccessId}",
            request.PerformedByUserId, request.IsAdmin, request.IsUser, request.AccessId);

        var record = await _access.FirstOrDefaultAsync(a => a.Id == request.AccessId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        // ── Authorisation ────────────────────────────────────────────────────
        string performedByRole;
        if (request.IsAdmin)
        {
            performedByRole = "ADMIN";
        }
        else if (request.IsUser)
        {
            if (record.UserId != request.PerformedByUserId)
                throw new ForbiddenException("You can only pause your own enrollments.");
            performedByRole = "USER";
        }
        else
        {
            // Expert path
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.PerformedByUserId && !e.IsDeleted && e.IsActive, cancellationToken)
                ?? throw new ForbiddenException("You do not have an active expert profile.");

            if (expert.Id != record.ExpertId)
                throw new ForbiddenException("You can only pause enrollments for your own programs.");

            performedByRole = "EXPERT";
        }

        // ── State guard ───────────────────────────────────────────────────────
        if (record.Status != UserProgramAccessStatus.Active)
            throw new DomainException($"Cannot pause an enrollment that is currently {record.Status}. It must be ACTIVE.");

        // ── State transition ──────────────────────────────────────────────────
        var now = DateTimeOffset.UtcNow;
        record.Status    = UserProgramAccessStatus.Paused;
        record.PausedAt  = now;
        record.UpdatedAt = now;

        await _sessionLogs.AddAsync(new ProgramSessionLog
        {
            Id              = Guid.NewGuid(),
            AccessId        = record.Id,
            Action          = SessionAction.Paused,
            PerformedBy     = request.PerformedByUserId,
            PerformedByRole = performedByRole,
            Note            = request.Note,
            CreatedAt       = now
        });

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("PauseEnrollment: access {AccessId} → PAUSED", record.Id);

        // ── Notify enrolled user ──────────────────────────────────────────────
        await SendSessionEmailAsync(record, "session_paused", cancellationToken);
    }

    private async Task SendSessionEmailAsync(
        UserProgramAccess record,
        string templateKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var enrolledUser = await _users.FirstOrDefaultAsync(u => u.Id == record.UserId, cancellationToken);
            if (enrolledUser is null) return;

            await _emailService.SendAsync(
                toEmail:      enrolledUser.Email,
                toName:       $"{enrolledUser.FirstName} {enrolledUser.LastName}",
                templateKey:  templateKey,
                templateData: new Dictionary<string, object>
                {
                    ["first_name"] = enrolledUser.FirstName
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("PauseEnrollment: '{Template}' email sent to user {UserId}", templateKey, enrolledUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PauseEnrollment: failed to send '{Template}' email — enrollment update is still saved", templateKey);
        }
    }
}
