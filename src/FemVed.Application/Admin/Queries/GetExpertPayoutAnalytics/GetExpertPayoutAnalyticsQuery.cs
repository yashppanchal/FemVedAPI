using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetExpertPayoutAnalytics;

/// <summary>
/// Returns a balance sheet for every expert: gross revenue earned, expert share,
/// platform commission, total already paid, and outstanding balance.
/// Used for the admin payout management dashboard.
/// </summary>
public record GetExpertPayoutAnalyticsQuery : IRequest<List<ExpertPayoutBalanceDto>>;
