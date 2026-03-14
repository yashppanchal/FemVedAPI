namespace FemVed.Infrastructure.Payment.Options;

/// <summary>Typed configuration for the Stripe Payments API.</summary>
public sealed class StripeOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Stripe";

    /// <summary>Stripe secret key (sk_live_xxx or sk_test_xxx).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Stripe webhook signing secret from the Dashboard (whsec_xxx). Used to verify webhook signatures.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Frontend return URL after successful Stripe Checkout.
    /// The orderId query param is appended automatically.
    /// </summary>
    public string ReturnUrl { get; set; } = "https://femved.com/payment/processing";

    /// <summary>Frontend cancel URL if the user abandons Stripe Checkout. The orderId query param is appended automatically.</summary>
    public string CancelUrl { get; set; } = "https://femved.com/payment/cancel";
}
