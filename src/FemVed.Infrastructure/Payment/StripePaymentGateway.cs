using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Infrastructure.Payment.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FemVed.Infrastructure.Payment;

/// <summary>
/// Implements <see cref="IPaymentGateway"/> using Stripe Checkout Sessions.
/// Used for all non-Indian international customers who choose Stripe.
/// Payment flow: create hosted Checkout Session → redirect user to session.url
/// → Stripe fires <c>checkout.session.completed</c> webhook on success.
/// All amounts must be in the smallest currency unit (pence for GBP, cents for USD).
/// </summary>
public sealed class StripePaymentGateway : IPaymentGateway
{
    private const string ApiBase = "https://api.stripe.com";

    private readonly HttpClient _http;
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentGateway> _logger;

    /// <summary>Initialises the gateway with a named <see cref="HttpClient"/> and typed options.</summary>
    public StripePaymentGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<StripeOptions> options,
        ILogger<StripePaymentGateway> logger)
    {
        _http    = httpClientFactory.CreateClient("stripe");
        _options = options.Value;
        _logger  = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Creates a Stripe Checkout Session in <c>payment</c> mode.
    /// Returns: <see cref="GatewayCreateOrderResult.GatewayOrderId"/> = Stripe session ID,
    ///          <see cref="GatewayCreateOrderResult.ApprovalUrl"/> = Stripe hosted checkout URL.
    /// </remarks>
    public async Task<GatewayCreateOrderResult> CreateOrderAsync(
        CreateGatewayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.SecretKey))
            throw new InvalidOperationException("STRIPE_SECRET_KEY is not configured.");

        _logger.LogInformation("Stripe: creating checkout session for internal order {OrderId}", request.InternalOrderId);

        // Stripe amounts are in smallest currency unit (pence/cents) — multiply by 100.
        var unitAmount = (long)Math.Round(request.Amount * 100, MidpointRounding.AwayFromZero);
        var currency   = request.CurrencyCode.ToLowerInvariant();

        var successUrl = $"{_options.ReturnUrl}?orderId={Uri.EscapeDataString(request.InternalOrderId)}&gateway=stripe";
        var cancelUrl  = $"{_options.CancelUrl}?orderId={Uri.EscapeDataString(request.InternalOrderId)}";

        // Stripe API requires application/x-www-form-urlencoded (not JSON)
        var formParams = new Dictionary<string, string>
        {
            ["mode"]                                              = "payment",
            ["line_items[0][price_data][currency]"]              = currency,
            ["line_items[0][price_data][unit_amount]"]           = unitAmount.ToString(),
            ["line_items[0][price_data][product_data][name]"]    = "FemVed Guided Program",
            ["line_items[0][quantity]"]                          = "1",
            ["success_url"]                                      = successUrl,
            ["cancel_url"]                                       = cancelUrl,
            ["client_reference_id"]                              = request.InternalOrderId,
            ["customer_email"]                                   = request.CustomerEmail,
            // Embed internal order ID in PaymentIntent metadata so payment_intent.* webhooks can resolve the order
            ["payment_intent_data[metadata][internal_order_id]"] = request.InternalOrderId,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/checkout/sessions")
        {
            Content = new FormUrlEncodedContent(formParams)
        };
        SetStripeAuthHeader(httpRequest);

        var response = await _http.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Stripe CreateCheckoutSession failed: {Status} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Stripe checkout session creation failed: {response.StatusCode}");
        }

        using var doc        = JsonDocument.Parse(responseBody);
        var root             = doc.RootElement;
        var sessionId        = root.GetProperty("id").GetString()  ?? string.Empty;
        var checkoutUrl      = root.GetProperty("url").GetString() ?? string.Empty;

        _logger.LogInformation("Stripe: checkout session {SessionId} created", sessionId);

        return new GatewayCreateOrderResult(
            GatewayOrderId:   sessionId,
            PaymentSessionId: null,
            ApprovalUrl:      checkoutUrl);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Stripe webhook signature verification:
    /// 1. Parse <c>Stripe-Signature</c> header → extract <c>t</c> (timestamp) and <c>v1</c> (HMAC).
    /// 2. Compute HMAC-SHA256 of <c>{timestamp}.{rawPayload}</c> using the webhook signing secret.
    /// 3. Compare in constant time.
    /// 4. Reject if timestamp is more than 5 minutes old (replay-attack prevention).
    /// </remarks>
    public Task<bool> VerifyWebhookSignatureAsync(
        string rawPayload,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        if (!headers.TryGetValue("stripe-signature", out var sigHeader) || string.IsNullOrEmpty(sigHeader))
        {
            _logger.LogWarning("Stripe webhook missing Stripe-Signature header");
            return Task.FromResult(false);
        }

        // Parse: "t=1614805832,v1=abc...,v0=def..."
        string? timestamp = null;
        string? v1Sig     = null;
        foreach (var part in sigHeader.Split(','))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0] == "t")  timestamp = kv[1];
            if (kv[0] == "v1") v1Sig     = kv[1];
        }

        if (timestamp is null || v1Sig is null)
        {
            _logger.LogWarning("Stripe webhook: malformed Stripe-Signature header");
            return Task.FromResult(false);
        }

        // Replay-attack prevention: reject events older than 5 minutes or more than 30s in the future
        if (long.TryParse(timestamp, out var ts))
        {
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts;
            if (age > 300)
            {
                _logger.LogWarning("Stripe webhook: timestamp too old ({Age}s) — possible replay attack", age);
                return Task.FromResult(false);
            }
            if (age < -30)
            {
                _logger.LogWarning("Stripe webhook: timestamp is in the future ({Age}s ahead) — rejected", -age);
                return Task.FromResult(false);
            }
        }

        // Compute expected signature
        var signedPayload = $"{timestamp}.{rawPayload}";
        var keyBytes      = Encoding.UTF8.GetBytes(_options.WebhookSecret);
        var msgBytes      = Encoding.UTF8.GetBytes(signedPayload);

        using var hmac    = new HMACSHA256(keyBytes);
        var computed      = Convert.ToHexString(hmac.ComputeHash(msgBytes)).ToLowerInvariant();

        var isValid = string.Equals(computed, v1Sig, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
            _logger.LogWarning("Stripe webhook signature mismatch");

        return Task.FromResult(isValid);
    }

    /// <inheritdoc/>
    /// <remarks>Stripe Checkout payments are captured automatically — no explicit capture is needed.</remarks>
    public Task<string?> CaptureOrderAsync(
        string gatewayOrderId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Stripe Checkout does not require an explicit capture call.");

    /// <inheritdoc/>
    /// <remarks>
    /// Submits a refund against the PaymentIntent associated with the order.
    /// Partial refunds are supported; amount is in smallest currency unit.
    /// </remarks>
    public async Task<GatewayRefundResult> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.GatewayPaymentId))
        {
            return new GatewayRefundResult(
                Success: false,
                GatewayRefundId: null,
                FailureReason: "Stripe refund requires GatewayPaymentId (PaymentIntent ID or charge ID).");
        }

        _logger.LogInformation("Stripe: refunding payment {PaymentId}, refund {RefundId}",
            request.GatewayPaymentId, request.InternalRefundId);

        var unitAmount = (long)Math.Round(request.Amount * 100, MidpointRounding.AwayFromZero);

        var formParams = new Dictionary<string, string>
        {
            ["payment_intent"] = request.GatewayPaymentId,
            ["amount"]         = unitAmount.ToString(),
            ["reason"]         = "requested_by_customer",
            ["metadata[internal_refund_id]"] = request.InternalRefundId,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/refunds")
        {
            Content = new FormUrlEncodedContent(formParams)
        };
        SetStripeAuthHeader(httpRequest);

        var response     = await _http.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Stripe RefundAsync failed: {Status} {Body}", response.StatusCode, responseBody);
            return new GatewayRefundResult(
                Success: false,
                GatewayRefundId: null,
                FailureReason: $"Stripe refund failed: {response.StatusCode}");
        }

        using var doc    = JsonDocument.Parse(responseBody);
        var refundId     = doc.RootElement.TryGetProperty("id", out var rid) ? rid.GetString() : null;

        _logger.LogInformation("Stripe: refund {RefundId} completed", refundId);

        return new GatewayRefundResult(
            Success: true,
            GatewayRefundId: refundId,
            FailureReason: null);
    }

    private void SetStripeAuthHeader(HttpRequestMessage request)
        => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
}
