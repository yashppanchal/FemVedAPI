using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Enrollments.Commands.StartEnrollment;

/// <summary>
/// Handles <see cref="StartEnrollmentCommand"/>.
/// <list type="number">
///   <item>Verifies the enrollment exists and the caller is authorised to start it.</item>
///   <item>Guards against invalid state transitions (must be NOT_STARTED).</item>
///   <item>Sets Status = ACTIVE, StartedAt = now.</item>
///   <item>Appends a <see cref="ProgramSessionLog"/> entry.</item>
///   <item>Emails the enrolled user via the <c>session_started</c> SendGrid template.</item>
/// </list>
/// </summary>
public sealed class StartEnrollmentCommandHandler : IRequestHandler<StartEnrollmentCommand>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ProgramSessionLog> _sessionLogs;
    private readonly IRepository<User> _users;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<StartEnrollmentCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public StartEnrollmentCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<ProgramSessionLog> sessionLogs,
        IRepository<User> users,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<StartEnrollmentCommandHandler> logger)
    {
        _access      = access;
        _experts     = experts;
        _sessionLogs = sessionLogs;
        _users       = users;
        _emailService = emailService;
        _uow         = uow;
        _logger      = logger;
    }

    /// <summary>Starts the enrollment and notifies the enrolled user.</summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller is not the expert for this program.</exception>
    /// <exception cref="DomainException">Thrown when the enrollment is not in NOT_STARTED status.</exception>
    public async Task Handle(StartEnrollmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "StartEnrollment: user {UserId} (isAdmin={IsAdmin}) starting access {AccessId}",
            request.PerformedByUserId, request.IsAdmin, request.AccessId);

        var record = await _access.FirstOrDefaultAsync(a => a.Id == request.AccessId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        // ── Authorisation ────────────────────────────────────────────────────
        string performedByRole;
        if (request.IsAdmin)
        {
            performedByRole = "ADMIN";
        }
        else
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.PerformedByUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("You do not have an expert profile.");

            if (expert.Id != record.ExpertId)
                throw new ForbiddenException("You can only start enrollments for your own programs.");

            performedByRole = "EXPERT";
        }

        // ── State guard ───────────────────────────────────────────────────────
        if (record.Status != UserProgramAccessStatus.NotStarted)
            throw new DomainException($"Cannot start an enrollment that is currently {record.Status}. It must be NOT_STARTED.");

        // ── State transition ──────────────────────────────────────────────────
        var now = DateTimeOffset.UtcNow;
        record.Status    = UserProgramAccessStatus.Active;
        record.StartedAt = now;
        record.UpdatedAt = now;

        await _sessionLogs.AddAsync(new ProgramSessionLog
        {
            Id              = Guid.NewGuid(),
            AccessId        = record.Id,
            Action          = SessionAction.Started,
            PerformedBy     = request.PerformedByUserId,
            PerformedByRole = performedByRole,
            Note            = request.Note,
            CreatedAt       = now
        });

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("StartEnrollment: access {AccessId} → ACTIVE", record.Id);

        // ── Notify enrolled user ──────────────────────────────────────────────
        await SendSessionEmailAsync(record, "session_started", cancellationToken);
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

            _logger.LogInformation("StartEnrollment: '{Template}' email sent to user {UserId}", templateKey, enrolledUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartEnrollment: failed to send '{Template}' email — enrollment update is still saved", templateKey);
        }
    }
}
