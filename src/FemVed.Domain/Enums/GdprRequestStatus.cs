namespace FemVed.Domain.Enums;

/// <summary>Processing state of a GDPR data-erasure request.</summary>
public enum GdprRequestStatus
{
    Pending,
    Processing,
    Completed,
    Rejected
}
