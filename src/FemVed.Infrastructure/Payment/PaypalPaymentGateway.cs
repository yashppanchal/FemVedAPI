using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FemVed.Application.Interfaces;
using FemVed.Infrastructure.Payment.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FemVed.Infrastructure.Payment;

/// <summary>
/// Implements <see cref="IPaymentGateway"/> using the PayPal Orders API v2.
/// Used for all non-Indian customers (GB, US, etc.).
/// Caches the OAuth2 access token for 8 hours to minimise token fetch round-trips.
/// </summary>
public sealed class PaypalPaymentGateway : IPaymentGateway
{
    private const string TokenCacheKey = "paypal_access_token";

    private readonly HttpClient _http;
    private readonly PaypalOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PaypalPaymentGateway> _logger;

    /// <summary>Initialises the gateway with a named <see cref="HttpClient"/>, typed options, and cache.</summary>
    public PaypalPaymentGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<PaypalOptions> options,
        IMemoryCache cache,
        ILogger<PaypalPaymentGateway> logger)
    {
        _http = httpClientFactory.CreateClient("paypal");
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<GatewayCreateOrderResult> CreateOrderAsync(
        CreateGatewayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("PayPal: creating order for internal ID {OrderId}", request.InternalOrderId);

        var token = await GetAccessTokenAsync(cancellationToken);

        // Format amount as "0.00" (PayPal requires exactly 2 decimal places for most currencies)
        var amountValue = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = request.InternalOrderId,
                    custom_id    = request.InternalOrderId,
                    amount = new
                    {
                        currency_code = request.CurrencyCode,
                        value = amountValue
                    }
                }
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        return_url = _options.ReturnUrl,
                        cancel_url = _options.CancelUrl
                    }
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v2/checkout/orders")
        {
            Content = JsonContent.Create(body)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal CreateOrder failed: {Status} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"PayPal order creation failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var paypalOrderId = root.GetProperty("id").GetString() ?? string.Empty;

        // Find the approval URL — PayPal returns "payer-action" when payment_source.paypal
        // is specified, and "approve" in the basic HATEOAS flow. Accept either.
        var approvalUrl = string.Empty;
        if (root.TryGetProperty("links", out var links))
        {
            foreach (var link in links.EnumerateArray())
            {
                if (!link.TryGetProperty("rel", out var rel)) continue;
                var relValue = rel.GetString();
                if (relValue == "payer-action" || relValue == "approve")
                {
                    approvalUrl = link.GetProperty("href").GetString() ?? string.Empty;
                    break;
                }
            }
        }

        _logger.LogInformation("PayPal: order {PayPalOrderId} created", paypalOrderId);

        return new GatewayCreateOrderResult(
            GatewayOrderId: paypalOrderId,
            PaymentSessionId: null,
            ApprovalUrl: approvalUrl);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Verification is performed by calling PayPal's
    /// <c>POST /v1/notifications/verify-webhook-signature</c> endpoint.
    /// Required headers: paypal-auth-algo, paypal-cert-url, paypal-transmission-id,
    /// paypal-transmission-sig, paypal-transmission-time.
    /// </remarks>
    public async Task<bool> VerifyWebhookSignatureAsync(
        string rawPayload,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        headers.TryGetValue("paypal-auth-algo", out var authAlgo);
        headers.TryGetValue("paypal-cert-url", out var certUrl);
        headers.TryGetValue("paypal-transmission-id", out var transmissionId);
        headers.TryGetValue("paypal-transmission-sig", out var transmissionSig);
        headers.TryGetValue("paypal-transmission-time", out var transmissionTime);

        if (string.IsNullOrEmpty(authAlgo) || string.IsNullOrEmpty(certUrl)
            || string.IsNullOrEmpty(transmissionId) || string.IsNullOrEmpty(transmissionSig))
        {
            _logger.LogWarning("PayPal webhook missing required verification headers");
            return false;
        }

        var token = await GetAccessTokenAsync(cancellationToken);

        // PayPal requires the raw event JSON parsed as an object in the verification body
        using var eventDoc = JsonDocument.Parse(rawPayload);
        var webhookEvent = eventDoc.RootElement;

        var verifyBody = new
        {
            auth_algo       = authAlgo,
            cert_url        = certUrl,
            transmission_id = transmissionId,
            transmission_sig= transmissionSig,
            transmission_time= transmissionTime,
            webhook_id      = _options.WebhookId,
            webhook_event   = webhookEvent
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post, "/v1/notifications/verify-webhook-signature")
        {
            Content = JsonContent.Create(verifyBody)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayPal verification API returned {Status}", response.StatusCode);
            return false;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var resultDoc = JsonDocument.Parse(responseBody);
        var status = resultDoc.RootElement
            .TryGetProperty("verification_status", out var vs)
                ? vs.GetString()
                : null;

        var isValid = string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase);

        if (!isValid)
            _logger.LogWarning("PayPal webhook verification_status: {Status}", status);

        return isValid;
    }

    /// <inheritdoc/>
    public async Task<GatewayRefundResult> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.GatewayPaymentId))
        {
            return new GatewayRefundResult(
                Success: false,
                GatewayRefundId: null,
                FailureReason: "PayPal refund requires GatewayPaymentId (capture ID).");
        }

        _logger.LogInformation("PayPal: refunding capture {CaptureId}, refund {RefundId}",
            request.GatewayPaymentId, request.InternalRefundId);

        var token = await GetAccessTokenAsync(cancellationToken);

        var amountValue = request.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var body = new
        {
            amount = new { value = amountValue, currency_code = request.CurrencyCode },
            note_to_payer = request.Reason
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post, $"/v2/payments/captures/{request.GatewayPaymentId}/refund")
        {
            Content = JsonContent.Create(body)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal RefundAsync failed: {Status} {Body}", response.StatusCode, responseBody);
            return new GatewayRefundResult(
                Success: false,
                GatewayRefundId: null,
                FailureReason: $"PayPal refund failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var refundId = doc.RootElement.TryGetProperty("id", out var rid) ? rid.GetString() : null;

        _logger.LogInformation("PayPal: refund {RefundId} completed", refundId);

        return new GatewayRefundResult(
            Success: true,
            GatewayRefundId: refundId,
            FailureReason: null);
    }

    // ── OAuth token management ────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cachedToken) && cachedToken is not null)
            return cachedToken;

        _logger.LogInformation("PayPal: fetching new access token");

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.Secret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token")
        {
            Content = new StringContent("grant_type=client_credentials",
                Encoding.UTF8, "application/x-www-form-urlencoded")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);

        var token = doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("PayPal access token response missing 'access_token'.");

        // Cache for 8 hours (token typically valid 9 hours)
        _cache.Set(TokenCacheKey, token, TimeSpan.FromHours(8));

        return token;
    }
}
