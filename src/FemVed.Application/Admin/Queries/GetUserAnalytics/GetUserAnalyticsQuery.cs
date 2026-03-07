using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetUserAnalytics;

/// <summary>
/// Returns user registration and purchase analytics: total users, buyers, repeat ratio,
/// conversion rate, monthly new-user trend, and 12-month cohort analysis.
/// </summary>
public record GetUserAnalyticsQuery : IRequest<UserAnalyticsDto>;
