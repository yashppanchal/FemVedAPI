namespace FemVed.Infrastructure.Payment.Options;

/// <summary>Typed configuration for the PayPal Orders API v2.</summary>
public sealed class PaypalOptions
{
    /// <summary>Configuration section name (unused — options are bound from flat env vars).</summary>
    public const string SectionName = "Paypal";

    /// <summary>PayPal API base URL, e.g. "https://api-m.sandbox.paypal.com".</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>PayPal OAuth2 client ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>PayPal OAuth2 client secret.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>PayPal webhook ID required for server-side signature verification.</summary>
    public string WebhookId { get; set; } = string.Empty;

    /// <summary>Return URL after payer approves the PayPal order.</summary>
    public string ReturnUrl { get; set; } = "https://femved.com/payment/success";

    /// <summary>Cancel URL if payer cancels the PayPal order.</summary>
    public string CancelUrl { get; set; } = "https://femved.com/payment/cancel";
}
