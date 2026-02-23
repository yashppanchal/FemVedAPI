using FemVed.Application.Admin.DTOs;
using FemVed.Application.Interfaces;
using FemVed.Domain.Entities;
using FemVed.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Admin.Queries.GetAdminSummary;

/// <summary>Handles <see cref="GetAdminSummaryQuery"/>. Aggregates platform statistics.</summary>
public sealed class GetAdminSummaryQueryHandler : IRequestHandler<GetAdminSummaryQuery, AdminSummaryDto>
{
    private readonly IRepository<User> _users;
    private readonly IRepository<Expert> _experts;
    private readonly IRepository<Program> _programs;
    private readonly IRepository<Order> _orders;
    private readonly IRepository<GdprDeletionRequest> _gdprRequests;
    private readonly IRepository<Coupon> _coupons;
    private readonly ILogger<GetAdminSummaryQueryHandler> _logger;

    /// <summary>Initialises the handler.</summary>
    public GetAdminSummaryQueryHandler(
        IRepository<User> users,
        IRepository<Expert> experts,
        IRepository<Program> programs,
        IRepository<Order> orders,
        IRepository<GdprDeletionRequest> gdprRequests,
        IRepository<Coupon> coupons,
        ILogger<GetAdminSummaryQueryHandler> logger)
    {
        _users        = users;
        _experts      = experts;
        _programs     = programs;
        _orders       = orders;
        _gdprRequests = gdprRequests;
        _coupons      = coupons;
        _logger       = logger;
    }

    /// <summary>Returns aggregated platform statistics.</summary>
    public async Task<AdminSummaryDto> Handle(GetAdminSummaryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetAdminSummary: computing platform statistics");

        var users      = await _users.GetAllAsync(cancellationToken: cancellationToken);
        var experts    = await _experts.GetAllAsync(cancellationToken: cancellationToken);
        var programs   = await _programs.GetAllAsync(cancellationToken: cancellationToken);
        var orders     = await _orders.GetAllAsync(cancellationToken: cancellationToken);
        var gdpr       = await _gdprRequests.GetAllAsync(cancellationToken: cancellationToken);
        var coupons    = await _coupons.GetAllAsync(cancellationToken: cancellationToken);

        var paidOrders = orders.Where(o => o.Status == OrderStatus.Paid).ToList();

        var summary = new AdminSummaryDto(
            TotalUsers:           users.Count(u => !u.IsDeleted),
            ActiveUsers:          users.Count(u => !u.IsDeleted && u.IsActive),
            TotalExperts:         experts.Count(e => !e.IsDeleted),
            TotalPrograms:        programs.Count(p => !p.IsDeleted),
            PublishedPrograms:    programs.Count(p => !p.IsDeleted && p.Status == ProgramStatus.Published),
            TotalOrders:          orders.Count,
            PaidOrders:           paidOrders.Count,
            TotalRevenue:         paidOrders.Sum(o => o.AmountPaid),
            PendingGdprRequests:  gdpr.Count(r => r.Status == GdprRequestStatus.Pending),
            ActiveCoupons:        coupons.Count(c => c.IsActive));

        _logger.LogInformation("GetAdminSummary: completed");
        return summary;
    }
}
