using FemVed.Domain.Enums;

namespace FemVed.Domain.Entities;

/// <summary>
/// Purchase record for a program duration.
/// Idempotency: duplicate <see cref="IdempotencyKey"/> returns the existing order without creating a new one.
/// Payment webhook signatures must be verified before any DB update.
/// </summary>
public class Order
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the purchasing user.</summary>
    public Guid UserId { get; set; }

    /// <summary>FK to the duration being purchased.</summary>
    public Guid DurationId { get; set; }

    /// <summary>FK to the specific location-priced row used at time of purchase.</summary>
    public Guid DurationPriceId { get; set; }

    /// <summary>Final amount charged after any coupon discount.</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>ISO 4217 currency code, e.g. "GBP".</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Location code used to resolve pricing, e.g. "GB".</summary>
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>FK to the applied coupon. Null if no coupon was used.</summary>
    public Guid? CouponId { get; set; }

    /// <summary>Amount discounted by the coupon (0 if none applied).</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Current order lifecycle state.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Which payment gateway processed this order.</summary>
    public PaymentGateway PaymentGateway { get; set; }

    /// <summary>Client-generated UUID to prevent duplicate order creation.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Order ID returned by the payment gateway (CashFree order_id or PayPal order id).</summary>
    public string? GatewayOrderId { get; set; }

    /// <summary>Payment ID returned by the gateway after payment completion.</summary>
    public string? GatewayPaymentId { get; set; }

    /// <summary>Full webhook payload stored as JSON for audit purposes.</summary>
    public string? GatewayResponse { get; set; }

    /// <summary>Human-readable failure reason if status is FAILED.</summary>
    public string? FailureReason { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigations
    /// <summary>The user who placed this order.</summary>
    public User User { get; set; } = null!;

    /// <summary>The duration purchased.</summary>
    public ProgramDuration Duration { get; set; } = null!;

    /// <summary>The price snapshot used at checkout.</summary>
    public DurationPrice DurationPrice { get; set; } = null!;

    /// <summary>The coupon applied (null if none).</summary>
    public Coupon? Coupon { get; set; }

    /// <summary>Refunds issued against this order.</summary>
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    /// <summary>Program access records created when this order is paid.</summary>
    public ICollection<UserProgramAccess> ProgramAccesses { get; set; } = new List<UserProgramAccess>();
}
