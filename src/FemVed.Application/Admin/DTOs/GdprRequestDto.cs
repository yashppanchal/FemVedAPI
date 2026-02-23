namespace FemVed.Application.Admin.DTOs;

/// <summary>GDPR right-to-erasure request for admin processing.</summary>
/// <param name="RequestId">Request UUID.</param>
/// <param name="UserId">UUID of the user who submitted the request.</param>
/// <param name="UserEmail">Email of the requesting user.</param>
/// <param name="UserFirstName">First name of the requesting user.</param>
/// <param name="UserLastName">Last name of the requesting user.</param>
/// <param name="Status">Current processing state: Pending, Processing, Completed, or Rejected.</param>
/// <param name="RequestedAt">UTC timestamp when the request was submitted.</param>
/// <param name="CompletedAt">UTC timestamp when the request was fulfilled or rejected. Null if still open.</param>
/// <param name="RejectionReason">Reason provided when rejecting. Null if not rejected.</param>
/// <param name="ProcessedByUserId">UUID of the Admin who processed this request. Null if unprocessed.</param>
public record GdprRequestDto(
    Guid RequestId,
    Guid UserId,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    string? RejectionReason,
    Guid? ProcessedByUserId);
