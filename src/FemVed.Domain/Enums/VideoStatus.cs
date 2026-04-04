namespace FemVed.Domain.Enums;

/// <summary>Lifecycle states for a library video. Flow: DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED.</summary>
public enum VideoStatus
{
    Draft,
    PendingReview,
    Published,
    Archived
}
