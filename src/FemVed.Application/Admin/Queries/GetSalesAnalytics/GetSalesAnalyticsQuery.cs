using FemVed.Application.Admin.DTOs;
using MediatR;

namespace FemVed.Application.Admin.Queries.GetSalesAnalytics;

/// <summary>
/// Returns aggregated sales analytics: revenue by currency, gateway, country, and month,
/// plus order funnel counts and discount totals. Used for the admin revenue dashboard.
/// </summary>
public record GetSalesAnalyticsQuery : IRequest<SalesAnalyticsDto>;
