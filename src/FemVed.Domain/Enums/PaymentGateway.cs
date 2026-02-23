namespace FemVed.Domain.Enums;

/// <summary>Payment processor used for an order. Selection is based on country_iso_code: IN → CashFree, others → PayPal.</summary>
public enum PaymentGateway
{
    CashFree,
    PayPal
}
