using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Users.Commands.RequestGdprDeletion;

/// <summary>
/// Handles <see cref="RequestGdprDeletionCommand"/>.
/// Creates a new <see cref="GdprDeletionRequest"/> record with status Pending.
/// If a Pending request already exists for this user, the command is a no-op
/// (prevents duplicate submissions).
/// </summary>
public sealed class RequestGdprDeletionCommandHandler : IRequestHandler<RequestGdprDeletionCommand>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<GdprDeletionRequest> _gdprRequests;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RequestGdprDeletionCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public RequestGdprDeletionCommandHandler(
        IRepository<User> users,
        IRepository<GdprDeletionRequest> gdprRequests,
        IUnitOfWork uow,
        ILogger<RequestGdprDeletionCommandHandler> logger)
    {
        _users        = users;
        _gdprRequests = gdprRequests;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Submits a GDPR erasure request or silently skips if one is already pending.</summary>
    /// <param name="request">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the user record does not exist.</exception>
    public async Task Handle(RequestGdprDeletionCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("RequestGdprDeletion: received from user {UserId}", request.UserId);

        var user = await _users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        // Idempotency: if a Pending request already exists, do nothing
        var existingPending = await _gdprRequests.FirstOrDefaultAsync(
            r => r.UserId == request.UserId && r.Status == GdprRequestStatus.Pending,
            cancellationToken);

        if (existingPending is not null)
        {
            _logger.LogInformation("RequestGdprDeletion: pending request already exists for user {UserId} — skipping", request.UserId);
            return;
        }

        var gdprRequest = new GdprDeletionRequest
        {
            Id          = Guid.NewGuid(),
            UserId      = user.Id,
            RequestedAt = DateTimeOffset.UtcNow,
            Status      = GdprRequestStatus.Pending
        };

        await _gdprRequests.AddAsync(gdprRequest);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("RequestGdprDeletion: request {RequestId} created for user {UserId}", gdprRequest.Id, request.UserId);
    }
}
