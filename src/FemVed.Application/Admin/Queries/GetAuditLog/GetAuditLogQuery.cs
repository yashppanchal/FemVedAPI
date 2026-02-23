using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetAuditLog;

/// <summary>
/// Returns the most recent audit log entries, ordered by timestamp descending.
/// </summary>
/// <param name="Limit">Maximum number of entries to return. Defaults to 100.</param>
public record GetAuditLogQuery(int Limit = 100) : IRequest<List<AuditLogDto>>;
