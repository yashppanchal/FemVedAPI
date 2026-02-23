namespace FemVed.Domain.Enums;

/// <summary>Payment order lifecycle states.</summary>
public enum OrderStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}
