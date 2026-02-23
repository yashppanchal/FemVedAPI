namespace FemVed.Domain.Enums;

/// <summary>Lifecycle states for a guided program. Flow: DRAFT → PENDING_REVIEW → PUBLISHED → ARCHIVED.</summary>
public enum ProgramStatus
{
    Draft,
    PendingReview,
    Published,
    Archived
}
