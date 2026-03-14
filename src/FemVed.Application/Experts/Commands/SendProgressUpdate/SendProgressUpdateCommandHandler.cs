using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Experts.Commands.SendProgressUpdate;

/// <summary>
/// Handles <see cref="SendProgressUpdateCommand"/>.
/// <list type="number">
///   <item>Verifies the access record exists and the caller is authorised (expert owns program, or admin).</item>
///   <item>Resolves the expert ID: for experts, from their profile; for admins, uses the access record's ExpertId.</item>
///   <item>Persists an <see cref="ExpertProgressUpdate"/> record.</item>
///   <item>Always sends the comment as an email via SendGrid (<c>expert_progress_update</c> template).</item>
/// </list>
/// Email failures are caught and logged — they never fail the overall operation.
/// </summary>
public sealed class SendProgressUpdateCommandHandler : IRequestHandler<SendProgressUpdateCommand>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<ExpertProgressUpdate> _progressUpdates;
    private readonly IRepository<User> _users;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SendProgressUpdateCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public SendProgressUpdateCommandHandler(
        IRepository<UserProgramAccess> access,
        IRepository<Expert> experts,
        IRepository<ExpertProgressUpdate> progressUpdates,
        IRepository<User> users,
        IEmailService emailService,
        IUnitOfWork uow,
        ILogger<SendProgressUpdateCommandHandler> logger)
    {
        _access          = access;
        _experts         = experts;
        _progressUpdates = progressUpdates;
        _users           = users;
        _emailService    = emailService;
        _uow             = uow;
        _logger          = logger;
    }

    /// <summary>Persists the comment and emails the enrolled user.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the caller is not the expert for this program.</exception>
    public async Task Handle(SendProgressUpdateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SendProgressUpdate: user {UserId} (isAdmin={IsAdmin}) sending comment for access {AccessId}",
            request.SenderUserId, request.IsAdmin, request.AccessId);

        var accessRecord = await _access.FirstOrDefaultAsync(
            a => a.Id == request.AccessId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        // ── Authorisation + resolve expert ID for the record ──────────────────
        Guid recordExpertId;
        if (request.IsAdmin)
        {
            // Admin can comment on any enrollment; the stored ExpertId is the program's expert
            recordExpertId = accessRecord.ExpertId;
        }
        else
        {
            var expert = await _experts.FirstOrDefaultAsync(
                e => e.UserId == request.SenderUserId && !e.IsDeleted, cancellationToken)
                ?? throw new ForbiddenException("You do not have an expert profile.");

            if (expert.Id != accessRecord.ExpertId)
                throw new ForbiddenException("You can only send comments for your own enrolled users.");

            recordExpertId = expert.Id;
        }

        // ── Persist comment ───────────────────────────────────────────────────
        var update = new ExpertProgressUpdate
        {
            Id         = Guid.NewGuid(),
            AccessId   = request.AccessId,
            ExpertId   = recordExpertId,
            UpdateNote = request.UpdateNote.Trim(),
            SendEmail  = true,              // always email as per platform policy
            CreatedAt  = DateTimeOffset.UtcNow
        };

        await _progressUpdates.AddAsync(update);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SendProgressUpdate: comment {CommentId} persisted for access {AccessId}", update.Id, request.AccessId);

        // ── Always email the enrolled user ────────────────────────────────────
        try
        {
            var enrolledUser = await _users.FirstOrDefaultAsync(
                u => u.Id == accessRecord.UserId,
                cancellationToken);

            if (enrolledUser is not null)
            {
                await _emailService.SendAsync(
                    toEmail:      enrolledUser.Email,
                    toName:       $"{enrolledUser.FirstName} {enrolledUser.LastName}",
                    templateKey:  "expert_progress_update",
                    templateData: new Dictionary<string, object>
                    {
                        ["firstName"]  = enrolledUser.FirstName,
                        ["updateNote"] = request.UpdateNote,
                        ["year"]       = DateTimeOffset.UtcNow.Year.ToString()
                    },
                    cancellationToken: cancellationToken);

                _logger.LogInformation("SendProgressUpdate: email sent to user {UserId}", enrolledUser.Id);
            }
            else
            {
                _logger.LogWarning("SendProgressUpdate: enrolled user {UserId} not found, skipping email", accessRecord.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendProgressUpdate: email failed for access {AccessId} — comment is still saved", request.AccessId);
        }

        _logger.LogInformation("SendProgressUpdate: completed for access {AccessId}", request.AccessId);
    }
}
