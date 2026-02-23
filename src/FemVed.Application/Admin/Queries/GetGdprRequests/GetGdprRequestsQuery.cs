using FemVed.Application.Admin.DTOs;
using FemVed.Domain.Enums;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetGdprRequests;

/// <summary>
/// Returns GDPR erasure requests filtered by status, ordered by submission date descending.
/// Defaults to <see cref="GdprRequestStatus.Pending"/> to surface work items first.
/// </summary>
/// <param name="Status">Optional status filter. Null returns all requests.</param>
public record GetGdprRequestsQuery(GdprRequestStatus? Status = GdprRequestStatus.Pending)
    : IRequest<List<GdprRequestDto>>;
