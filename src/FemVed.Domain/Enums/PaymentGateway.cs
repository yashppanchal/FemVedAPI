namespace FemVed.Domain.Enums;

/// <summary>Payment processor used for an order. Selection is based on country_iso_code: IN → CashFree, others → PayPal or Stripe (user choice).</summary>
public enum PaymentGateway
{
    CashFree,
    PayPal,
    Stripe
}
