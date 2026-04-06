using FemVed.Application.Library.DTOs;
using MediatR;

namespace FemVed.Application.Library.Queries.GetLibraryAnalytics;

/// <summary>
/// Returns aggregated analytics for the Wellness Library: video counts,
/// purchase stats, revenue by currency, top-selling videos, and monthly trend.
/// Admin only.
/// </summary>
public record GetLibraryAnalyticsQuery : IRequest<LibraryAnalyticsDto>;
