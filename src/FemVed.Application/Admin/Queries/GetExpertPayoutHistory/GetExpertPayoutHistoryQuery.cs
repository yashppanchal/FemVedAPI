using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetExpertPayoutHistory;

/// <summary>
/// Returns the full payout history for a specific expert — every recorded payment,
/// newest first. Used for the per-expert payout ledger page.
/// </summary>
/// <param name="ExpertId">UUID of the expert profile to fetch history for.</param>
public record GetExpertPayoutHistoryQuery(Guid ExpertId) : IRequest<List<ExpertPayoutRecordDto>>;
