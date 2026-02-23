namespace FemVed.Application.Admin.DTOs;

/// <summary>High-level platform statistics for the admin dashboard summary card.</summary>
/// <param name="TotalUsers">Total non-deleted user accounts.</param>
/// <param name="ActiveUsers">Users with IsActive = true.</param>
/// <param name="TotalExperts">Total non-deleted expert profiles.</param>
/// <param name="TotalPrograms">Total non-deleted programs (all statuses).</param>
/// <param name="PublishedPrograms">Programs with status Published.</param>
/// <param name="TotalOrders">Total order records.</param>
/// <param name="PaidOrders">Orders with status Paid.</param>
/// <param name="TotalRevenue">Sum of AmountPaid across all Paid orders (in mixed currencies — informational only).</param>
/// <param name="PendingGdprRequests">GDPR erasure requests awaiting processing.</param>
/// <param name="ActiveCoupons">Coupons with IsActive = true.</param>
public record AdminSummaryDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalExperts,
    int TotalPrograms,
    int PublishedPrograms,
    int TotalOrders,
    int PaidOrders,
    decimal TotalRevenue,
    int PendingGdprRequests,
    int ActiveCoupons);
