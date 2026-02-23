using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>
/// GDPR right-to-erasure request from a UK/EU user.
/// Submitted via <c>POST /api/v1/users/me/gdpr-deletion-request</c>.
/// Processed manually by an Admin.
/// </summary>
public class GdprDeletionRequest
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the user requesting erasure.</summary>
    public Guid UserId { get; set; }

    /// <summary>UTC timestamp when the request was submitted.</summary>
    public DateTimeOffset RequestedAt { get; set; }

    /// <summary>Current processing state.</summary>
    public GdprRequestStatus Status { get; set; } = GdprRequestStatus.Pending;

    /// <summary>UTC timestamp when the request was fulfilled or rejected.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Reason provided by the Admin when rejecting a request.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>FK to the Admin who processed this request.</summary>
    public Guid? ProcessedBy { get; set; }

    // Navigations
    /// <summary>The user who submitted the erasure request.</summary>
    public User User { get; set; } = null!;

    /// <summary>The Admin who processed the request (null if unprocessed).</summary>
    public User? ProcessedByUser { get; set; }
}
