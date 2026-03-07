namespace FemVed.Domain.Entities;

/// <summary>
/// Records a payment made by the platform to an expert.
/// Used to track the outstanding balance owed to each expert.
/// Created by an admin each time funds are transferred.
/// </summary>
public class ExpertPayout
{
    /// <summary>Primary key (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>FK to the expert being paid.</summary>
    public Guid ExpertId { get; set; }

    /// <summary>Amount transferred to the expert in this payment.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO 4217 currency code for this payment, e.g. "GBP", "INR".</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Optional bank wire reference, PayPal transaction ID, etc.</summary>
    public string? PaymentReference { get; set; }

    /// <summary>Optional admin notes about this payment.</summary>
    public string? Notes { get; set; }

    /// <summary>FK to the admin user who recorded this payout.</summary>
    public Guid PaidBy { get; set; }

    /// <summary>UTC timestamp when the funds were transferred.</summary>
    public DateTimeOffset PaidAt { get; set; }

    /// <summary>UTC timestamp when this record was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigations
    /// <summary>The expert who received this payment.</summary>
    public Expert Expert { get; set; } = null!;

    /// <summary>The admin user who recorded this payment.</summary>
    public User PaidByUser { get; set; } = null!;
}
