namespace FemVed.Application.Admin.DTOs;

/// <summary>Audit log entry for the admin audit trail view.</summary>
/// <param name="LogId">Audit log entry UUID.</param>
/// <param name="AdminUserId">UUID of the Admin (or Expert) who performed the action.</param>
/// <param name="AdminEmail">Email of the Admin who performed the action.</param>
/// <param name="Action">Action code, e.g. "DEACTIVATE_USER", "CREATE_COUPON".</param>
/// <param name="EntityType">Affected table/entity type, e.g. "users", "coupons".</param>
/// <param name="EntityId">Primary key of the affected row. Null for bulk operations.</param>
/// <param name="BeforeValue">JSON snapshot of the entity before the change.</param>
/// <param name="AfterValue">JSON snapshot of the entity after the change.</param>
/// <param name="IpAddress">IP address of the client who made the request.</param>
/// <param name="CreatedAt">UTC timestamp of the action.</param>
public record AuditLogDto(
    Guid LogId,
    Guid AdminUserId,
    string AdminEmail,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? BeforeValue,
    string? AfterValue,
    string? IpAddress,
    DateTimeOffset CreatedAt);
