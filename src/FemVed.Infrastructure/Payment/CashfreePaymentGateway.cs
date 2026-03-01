using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FemVed.Application.Interfaces;
using FemVed.Infrastructure.Payment.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FemVed.Infrastructure.Payment;

/// <summary>
/// Implements <see cref="IPaymentGateway"/> using the CashFree Payments API (v2023-08-01).
/// Used for Indian customers (location code "IN", currency "INR").
/// </summary>
public sealed class CashfreePaymentGateway : IPaymentGateway
{
    private const string ApiVersion = "2023-08-01";

    private readonly HttpClient _http;
    private readonly CashfreeOptions _options;
    private readonly ILogger<CashfreePaymentGateway> _logger;

    /// <summary>Initialises the gateway with a named <see cref="HttpClient"/> and typed options.</summary>
    public CashfreePaymentGateway(
        IHttpClientFactory httpClientFactory,
        IOptions<CashfreeOptions> options,
        ILogger<CashfreePaymentGateway> logger)
    {
        _http = httpClientFactory.CreateClient("cashfree");
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<GatewayCreateOrderResult> CreateOrderAsync(
        CreateGatewayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CashFree: creating order for internal ID {OrderId}", request.InternalOrderId);

        var body = new
        {
            order_id = request.InternalOrderId,
            order_amount = request.Amount,
            order_currency = request.CurrencyCode,
            customer_details = new
            {
                customer_id = request.InternalOrderId,
                customer_email = request.CustomerEmail,
                customer_name = request.CustomerName,
                customer_phone = request.CustomerPhone ?? string.Empty
            },
            order_meta = new
            {
                return_url = _options.ReturnUrl.Replace("{order_id}", request.InternalOrderId)
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/orders")
        {
            Content = JsonContent.Create(body)
        };
        AddCashfreeHeaders(httpRequest);

        var response = await _http.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("CashFree CreateOrder failed: {Status} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"CashFree order creation failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var gatewayOrderId = root.GetProperty("cf_order_id").GetString() ?? string.Empty;
        var paymentSessionId = root.GetProperty("payment_session_id").GetString() ?? string.Empty;

        _logger.LogInformation("CashFree: order created. cf_order_id={GatewayOrderId}", gatewayOrderId);

        return new GatewayCreateOrderResult(
            GatewayOrderId: gatewayOrderId,
            PaymentSessionId: paymentSessionId,
            ApprovalUrl: null);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// CashFree signature = Base64( HMAC-SHA256( timestamp + rawBody, clientSecret ) )
    /// Headers expected: x-webhook-timestamp, x-webhook-signature.
    /// </remarks>
    public Task<bool> VerifyWebhookSignatureAsync(
        string rawPayload,
        IDictionary<string, string> headers,
        CancellationToken cancellationToken = default)
    {
        headers.TryGetValue("x-webhook-timestamp", out var timestamp);
        headers.TryGetValue("x-webhook-signature", out var signature);

        if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("CashFree webhook missing required headers");
            return Task.FromResult(false);
        }

        var message = $"{timestamp}{rawPayload}";
        var keyBytes = Encoding.UTF8.GetBytes(_options.ClientSecret);
        var msgBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(msgBytes);
        var computed = Convert.ToBase64String(hash);

        var isValid = string.Equals(computed, signature, StringComparison.Ordinal);

        if (!isValid)
            _logger.LogWarning("CashFree webhook signature mismatch");

        return Task.FromResult(isValid);
    }

    /// <inheritdoc/>
    /// <remarks>CashFree payments are captured automatically via the payment session — no explicit capture call needed.</remarks>
    public Task<string?> CaptureOrderAsync(
        string gatewayOrderId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("CashFree does not require an explicit capture call.");

    /// <inheritdoc/>
    public async Task<GatewayRefundResult> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CashFree: initiating refund for order {GatewayOrderId}, refund {RefundId}",
            request.GatewayOrderId, request.InternalRefundId);

        var body = new
        {
            refund_amount = request.Amount,
            refund_id = request.InternalRefundId,
            refund_note = request.Reason
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post, $"/orders/{request.GatewayOrderId}/refunds")
        {
            Content = JsonContent.Create(body)
        };
        AddCashfreeHeaders(httpRequest);

        var response = await _http.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("CashFree RefundAsync failed: {Status} {Body}", response.StatusCode, responseBody);
            return new GatewayRefundResult(
                Success: false,
                GatewayRefundId: null,
                FailureReason: $"CashFree refund failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var refundId = doc.RootElement.TryGetProperty("refund_id", out var rid) ? rid.GetString() : null;

        _logger.LogInformation("CashFree: refund {RefundId} initiated successfully", refundId);

        return new GatewayRefundResult(
            Success: true,
            GatewayRefundId: refundId,
            FailureReason: null);
    }

    private void AddCashfreeHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("x-client-id", _options.ClientId);
        request.Headers.Add("x-client-secret", _options.ClientSecret);
        request.Headers.Add("x-api-version", ApiVersion);
    }
}
