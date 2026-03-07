using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetExpertPayoutBalance;

/// <summary>
/// Returns the complete financial summary for a single expert:
/// total revenue collected, expert's payout share, platform profit,
/// amount already paid, and outstanding balance — all per currency.
/// </summary>
/// <param name="ExpertId">UUID of the expert profile to summarise.</param>
public record GetExpertPayoutBalanceQuery(Guid ExpertId) : IRequest<ExpertPayoutBalanceDto>;
