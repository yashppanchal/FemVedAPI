namespace FemVed.Infrastructure.Payment.Options;

/// <summary>Typed configuration for the CashFree Payments API.</summary>
public sealed class CashfreeOptions
{
    /// <summary>Configuration section name (unused — options are bound from flat env vars).</summary>
    public const string SectionName = "Cashfree";

    /// <summary>CashFree API base URL, e.g. "https://api.cashfree.com/pg".</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>CashFree API client ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>CashFree API client secret (used for HMAC webhook verification).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Return URL template sent to CashFree. Supports {order_id} placeholder.</summary>
    public string ReturnUrl { get; set; } = "https://femved.com/payment/return?order_id={order_id}";
}
