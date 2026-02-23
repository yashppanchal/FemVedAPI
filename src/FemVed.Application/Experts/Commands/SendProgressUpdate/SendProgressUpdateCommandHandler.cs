using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Commands.SendProgressUpdate;

/// <summary>
/// Handles <see cref="SendProgressUpdateCommand"/>.
/// <list type="number">
///   <item>Verifies the access record exists and belongs to the expert's own program.</item>
///   <item>Persists an <see cref="ExpertProgressUpdate"/> record.</item>
///   <item>Optionally sends an <c>expert_progress_update</c> email to the enrolled user via SendGrid.</item>
/// </list>
/// Email failures are caught and logged — they never fail the overall operation.
/// </summary>
public sealed class SendProgressUpdateCommandHandler : IRequestHandler<SendProgressUpdateCommand>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<ExpertProgressUpdate> _progressUpdates;
    private readonly IRepository<User> _users;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SendProgressUpdateCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public SendProgressUpdateCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<ExpertProgressUpdate> progressUpdates,
        IRepository<User> users,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<SendProgressUpdateCommandHandler> logger)
    {
        _access          = access;
        _progressUpdates = progressUpdates;
        _users           = users;
        _emailService    = emailService;
        _uow             = uow;
        _logger          = logger;
    }

    /// <summary>Persists the progress update and optionally emails the enrolled user.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the access record belongs to a different expert.</exception>
    public async Task Handle(SendProgressUpdateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SendProgressUpdate: expert {ExpertId} sending update for access {AccessId}",
            request.ExpertId, request.AccessId);

        // ── 1. Verify access record ownership ────────────────────────────────
        var accessRecord = await _access.FirstOrDefaultAsync(
            a => a.Id == request.AccessId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        if (accessRecord.ExpertId != request.ExpertId)
            throw new ForbiddenException("You can only send progress updates for your own enrolled users.");

        // ── 2. Persist ExpertProgressUpdate ──────────────────────────────────
        var update = new ExpertProgressUpdate
        {
            Id         = Guid.NewGuid(),
            AccessId   = request.AccessId,
            ExpertId   = request.ExpertId,
            UpdateNote = request.UpdateNote.Trim(),
            SendEmail  = request.SendEmail,
            CreatedAt  = DateTimeOffset.UtcNow
        };

        await _progressUpdates.AddAsync(update);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SendProgressUpdate: update {UpdateId} persisted", update.Id);

        // ── 3. Optionally email the enrolled user ─────────────────────────────
        if (request.SendEmail)
        {
            var enrolledUser = await _users.FirstOrDefaultAsync(
                u => u.Id == accessRecord.UserId,
                cancellationToken);

            if (enrolledUser is not null)
            {
                try
                {
                    await _emailService.SendAsync(
                        toEmail:      enrolledUser.Email,
                        toName:       $"{enrolledUser.FirstName} {enrolledUser.LastName}",
                        templateKey:  "expert_progress_update",
                        templateData: new Dictionary<string, object>
                        {
                            ["first_name"]   = enrolledUser.FirstName,
                            ["update_note"]  = request.UpdateNote
                        },
                        cancellationToken: cancellationToken);

                    _logger.LogInformation(
                        "SendProgressUpdate: email sent to user {UserId}", enrolledUser.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "SendProgressUpdate: email failed for user {UserId} — update is still saved",
                        enrolledUser.Id);
                }
            }
            else
            {
                _logger.LogWarning(
                    "SendProgressUpdate: enrolled user {UserId} not found, skipping email",
                    accessRecord.UserId);
            }
        }

        _logger.LogInformation(
            "SendProgressUpdate: completed for access {AccessId}", request.AccessId);
    }
}
