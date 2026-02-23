namespace FemVed.Domain.Entities;

/// <summary>
/// Immutable audit record written for every Admin or Expert mutation.
/// Stores before/after JSON snapshots for compliance and debugging.
/// </summary>
public class AdminAuditLog
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the Admin (or Expert) user who performed the action.</summary>
    public Guid AdminUserId { get; set; }

    /// <summary>Action code, e.g. "UPDATE_PRICE", "PUBLISH_PROGRAM".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Table/entity type affected, e.g. "programs", "duration_prices".</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Primary key of the affected entity row. Null for bulk operations.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>JSON snapshot of the entity before the change.</summary>
    public string? BeforeValue { get; set; }

    /// <summary>JSON snapshot of the entity after the change.</summary>
    public string? AfterValue { get; set; }

    /// <summary>IP address of the client making the request.</summary>
    public string? IpAddress { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    /// <summary>The admin user who performed the action.</summary>
    public User AdminUser { get; set; } = null!;
}
