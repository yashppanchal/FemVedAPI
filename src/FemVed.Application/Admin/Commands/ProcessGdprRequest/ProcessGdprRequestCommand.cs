using MediatR;

namespace FemVed.Application.Admin.Commands.ProcessGdprRequest;

/// <summary>
/// Marks a GDPR deletion request as Completed or Rejected and writes an audit log entry.
/// </summary>
public record ProcessGdprRequestCommand(
    Guid AdminUserId,
    string? IpAddress,
    Guid RequestId,
    /// <summary>"Complete" or "Reject".</summary>
    string Action,
    string? RejectionReason) : IRequest;
