using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Enrollments.Commands.RequestStartDate;

/// <summary>
/// Handles <see cref="RequestStartDateCommand"/>.
/// Records the user's preferred start date on the enrollment and sets
/// <c>StartRequestStatus = Pending</c>. The enrollment must be in <c>NotStarted</c> status.
/// </summary>
public sealed class RequestStartDateCommandHandler : IRequestHandler<RequestStartDateCommand>
{
    private readonly IRepository<UserProgramAccess> _access;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RequestStartDateCommandHandler> _logger;

    /// <summary>Initialises the handler with required services.</summary>
    public RequestStartDateCommandHandler(
        IRepository<UserProgramAccess> access,
        IUnitOfWork uow,
        ILogger<RequestStartDateCommandHandler> logger)
    {
        _access  = access;
        _uow     = uow;
        _logger  = logger;
    }

    /// <summary>Records the user's requested start date.</summary>
    /// <param name="request">The command payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the access record does not exist.</exception>
    /// <exception cref="ForbiddenException">Thrown when the user does not own this enrollment.</exception>
    /// <exception cref="DomainException">Thrown when the enrollment is not in NotStarted status.</exception>
    public async Task Handle(RequestStartDateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "RequestStartDate: user {UserId} requesting start date {Date} for access {AccessId}",
            request.UserId, request.RequestedStartDate, request.AccessId);

        var record = await _access.FirstOrDefaultAsync(a => a.Id == request.AccessId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProgramAccess), request.AccessId);

        if (record.UserId != request.UserId)
            throw new ForbiddenException("You can only request a start date for your own enrollments.");

        if (record.Status != UserProgramAccessStatus.NotStarted)
            throw new DomainException($"Cannot request a start date for an enrollment that is currently {record.Status}. It must be NOT_STARTED.");

        var now = DateTimeOffset.UtcNow;
        record.RequestedStartDate = new DateTimeOffset(
            request.RequestedStartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        record.StartRequestStatus = StartRequestStatus.Pending;
        record.UpdatedAt          = now;
        _access.Update(record);

        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "RequestStartDate: access {AccessId} now has RequestedStartDate={Date}, Status=Pending",
            record.Id, request.RequestedStartDate);
    }
}
