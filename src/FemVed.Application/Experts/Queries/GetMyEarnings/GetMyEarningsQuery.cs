using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Experts.Queries.GetMyEarnings;

/// <summary>
/// Returns the full earnings and payout balance sheet for the authenticated expert:
/// total revenue from their programs, their payout share (commissionRate %),
/// platform profit, total already paid, and outstanding balance — all per currency.
/// </summary>
/// <param name="UserId">The authenticated user's ID (from JWT). Used to resolve the expert profile.</param>
public record GetMyEarningsQuery(Guid UserId) : IRequest<ExpertPayoutBalanceDto>;
