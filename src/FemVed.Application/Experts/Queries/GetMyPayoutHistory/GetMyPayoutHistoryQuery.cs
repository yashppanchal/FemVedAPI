using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Experts.Queries.GetMyPayoutHistory;

/// <summary>
/// Returns all payout records received by the authenticated expert, newest first.
/// </summary>
/// <param name="UserId">The authenticated user's ID (from JWT). Used to resolve the expert profile.</param>
public record GetMyPayoutHistoryQuery(Guid UserId) : IRequest<List<ExpertPayoutRecordDto>>;
