using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>Refund record for a paid order. Initiated by an Admin.</summary>
public class Refund
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the order being refunded.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Amount to refund (may be partial).</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>Optional reason for the refund.</summary>
    public string? Reason { get; set; }

    /// <summary>Refund ID returned by the payment gateway.</summary>
    public string? GatewayRefundId { get; set; }

    /// <summary>Current refund processing state.</summary>
    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    /// <summary>FK to the Admin user who initiated the refund.</summary>
    public Guid InitiatedBy { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The order this refund is against.</summary>
    public Order Order { get; set; } = null!;

    /// <summary>The admin who triggered this refund.</summary>
    public User InitiatedByUser { get; set; } = null!;
}
