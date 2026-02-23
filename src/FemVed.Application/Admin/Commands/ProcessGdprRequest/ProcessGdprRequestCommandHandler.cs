using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using FemVed.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Commands.ProcessGdprRequest;

/// <summary>Handles <see cref="ProcessGdprRequestCommand"/>. Marks a GDPR request Complete or Rejected and writes an audit log entry.</summary>
public sealed class ProcessGdprRequestCommandHandler : IRequestHandler<ProcessGdprRequestCommand>
{
    private readonly IRepository<GdprDeletionRequest> _gdprRequests;
    private readonly IRepository<AdminAuditLog> _auditLogs;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProcessGdprRequestCommandHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public ProcessGdprRequestCommandHandler(
        IRepository<GdprDeletionRequest> gdprRequests,
        IRepository<AdminAuditLog> auditLogs,
        IUnitOfWork uow,
        ILogger<ProcessGdprRequestCommandHandler> logger)
    {
        _gdprRequests = gdprRequests;
        _auditLogs    = auditLogs;
        _uow          = uow;
        _logger       = logger;
    }

    /// <summary>Processes the GDPR request and logs the action.</summary>
    /// <exception cref="NotFoundException">Thrown when the request does not exist.</exception>
    /// <exception cref="DomainException">Thrown when the request is not in a Pending or Processing state.</exception>
    public async Task Handle(ProcessGdprRequestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProcessGdprRequest: admin {AdminId} processing GDPR request {RequestId} with action {Action}",
            request.AdminUserId, request.RequestId, request.Action);

        var gdprRequest = await _gdprRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(GdprDeletionRequest), request.RequestId);

        if (gdprRequest.Status == GdprRequestStatus.Completed || gdprRequest.Status == GdprRequestStatus.Rejected)
            throw new DomainException($"GDPR request {request.RequestId} has already been processed (status: {gdprRequest.Status}).");

        var before = JsonSerializer.Serialize(new { Status = gdprRequest.Status.ToString(), gdprRequest.CompletedAt, gdprRequest.RejectionReason });

        var isComplete = string.Equals(request.Action, "Complete", StringComparison.OrdinalIgnoreCase);
        gdprRequest.Status          = isComplete ? GdprRequestStatus.Completed : GdprRequestStatus.Rejected;
        gdprRequest.CompletedAt     = DateTimeOffset.UtcNow;
        gdprRequest.ProcessedBy     = request.AdminUserId;
        gdprRequest.RejectionReason = isComplete ? null : request.RejectionReason;
        _gdprRequests.Update(gdprRequest);

        var auditAction = isComplete ? "GDPR_REQUEST_COMPLETED" : "GDPR_REQUEST_REJECTED";

        await _auditLogs.AddAsync(new AdminAuditLog
        {
            Id          = Guid.NewGuid(),
            AdminUserId = request.AdminUserId,
            Action      = auditAction,
            EntityType  = "gdpr_deletion_requests",
            EntityId    = gdprRequest.Id,
            BeforeValue = before,
            AfterValue  = JsonSerializer.Serialize(new
            {
                Status          = gdprRequest.Status.ToString(),
                gdprRequest.CompletedAt,
                gdprRequest.RejectionReason
            }),
            IpAddress   = request.IpAddress,
            CreatedAt   = DateTimeOffset.UtcNow
        });

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ProcessGdprRequest: GDPR request {RequestId} marked {Status}", gdprRequest.Id, gdprRequest.Status);
    }
}
