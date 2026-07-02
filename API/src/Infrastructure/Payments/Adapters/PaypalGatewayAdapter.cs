using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Payments.Adapters;

/// <summary>
/// PayPal gateway adapter (WO-32) using the Orders v2 REST API over <see cref="IHttpClientFactory"/>.
/// Credentials are stored in the Credential Vault (service type "paypal") as <c>clientId:secret</c>; a
/// live/sandbox app credential is required. The adapter first exchanges the credential for an OAuth2
/// access token, then creates/authorizes/captures orders. <see cref="CaptureMode"/> maps to the order
/// <c>intent</c> (AUTHORIZE vs. CAPTURE).
/// </summary>
public class PaypalGatewayAdapter : PaymentGatewayAdapterBase
{
    private const string BaseUrl = "https://api-m.paypal.com";

    public PaypalGatewayAdapter(ICredentialVault vault, IHttpClientFactory httpClientFactory, ILogger<PaypalGatewayAdapter> logger)
        : base(vault, httpClientFactory, logger) { }

    public override PaymentMethodType Method => PaymentMethodType.PayPal;

    public override Task<PaymentResult> AuthorizeAsync(PaymentRequest req, CaptureMode mode, CancellationToken ct)
        => GuardAsync("authorize", async () =>
        {
            var token = await GetAccessTokenAsync(ct);
            if (token is null)
                return PaymentResult.Failed("PayPal credentials are not configured in the Credential Vault (service type 'paypal', format 'clientId:secret').");

            // Server-side completion of an order the buyer already approved on the client.
            if (!string.IsNullOrWhiteSpace(req.PaymentToken))
            {
                var action = mode == CaptureMode.AuthorizeOnly ? "authorize" : "capture";
                using var actDoc = await PostJsonAsync(token, $"{BaseUrl}/v2/checkout/orders/{req.PaymentToken}/{action}", "{}", ct);
                var actRoot = actDoc.RootElement;
                var actStatus = GetString(actRoot, "status");

                if (!string.Equals(actStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                    return PaymentResult.Failed($"PayPal order {action} returned status '{actStatus}'.");

                return mode == CaptureMode.AuthorizeOnly
                    ? PaymentResult.Authorized(ExtractPaymentId(actRoot, "authorizations") ?? req.PaymentToken, req.PaymentToken)
                    : PaymentResult.Captured(ExtractPaymentId(actRoot, "captures"), req.Amount, req.PaymentToken, req.PaymentToken);
            }

            // No approval token: create the order. The buyer approves via the returned link (redirect flow),
            // after which the order id is captured/authorized. The record stays Pending until then.
            var intent = mode == CaptureMode.AuthorizeOnly ? "AUTHORIZE" : "CAPTURE";
            var body = BuildCreateOrderJson(intent, req);
            using var doc = await PostJsonAsync(token, $"{BaseUrl}/v2/checkout/orders", body, ct);
            var root = doc.RootElement;

            var orderId = GetString(root, "id");
            if (string.IsNullOrWhiteSpace(orderId))
                return PaymentResult.Failed($"PayPal: {DescribeError(root)}");

            return new PaymentResult(true, PaymentStatus.Pending, AuthorizationId: orderId, GatewayReference: orderId);
        });

    public override Task<PaymentResult> CaptureAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("capture", async () =>
        {
            var token = await GetAccessTokenAsync(ct);
            if (token is null)
                return PaymentResult.Failed("PayPal credentials are not configured in the Credential Vault (service type 'paypal').");

            var orderId = payment.AuthorizationId ?? payment.GatewayReference;
            if (string.IsNullOrWhiteSpace(orderId))
                return PaymentResult.Failed("PayPal capture requires the original order id.");

            using var doc = await PostJsonAsync(token, $"{BaseUrl}/v2/checkout/orders/{orderId}/capture", "{}", ct);
            var root = doc.RootElement;
            var status = GetString(root, "status");

            return string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                ? PaymentResult.Captured(ExtractPaymentId(root, "captures"), amount, orderId, orderId)
                : PaymentResult.Failed($"PayPal capture returned status '{status}'.");
        });

    public override Task<PaymentResult> RefundAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("refund", async () =>
        {
            var token = await GetAccessTokenAsync(ct);
            if (token is null)
                return PaymentResult.Failed("PayPal credentials are not configured in the Credential Vault (service type 'paypal').");

            var captureId = payment.TransactionId;
            if (string.IsNullOrWhiteSpace(captureId))
                return PaymentResult.Failed("PayPal refund requires the capture id (TransactionId).");

            var body = JsonSerializer.Serialize(new
            {
                amount = new { value = amount.ToString("0.00", CultureInfo.InvariantCulture), currency_code = payment.CurrencyCode },
            });
            using var doc = await PostJsonAsync(token, $"{BaseUrl}/v2/payments/captures/{captureId}/refund", body, ct);
            var root = doc.RootElement;
            var status = GetString(root, "status");

            return status is "COMPLETED" or "PENDING"
                ? new PaymentResult(true, PaymentStatus.Refunded, TransactionId: GetString(root, "id"), GatewayReference: GetString(root, "id"), CapturedAmount: amount)
                : PaymentResult.Failed($"PayPal refund returned status '{status}'.");
        });

    private static string BuildCreateOrderJson(string intent, PaymentRequest req)
    {
        var order = new Dictionary<string, object?>
        {
            ["intent"] = intent,
            ["purchase_units"] = new[]
            {
                new { amount = new { currency_code = req.CurrencyCode, value = req.Amount.ToString("0.00", CultureInfo.InvariantCulture) } },
            },
        };
        if (!string.IsNullOrWhiteSpace(req.ReturnUrl))
            order["application_context"] = new { return_url = req.ReturnUrl };
        return JsonSerializer.Serialize(order);
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var credential = await ResolveCredentialAsync(ct);
        if (string.IsNullOrWhiteSpace(credential))
            return null;

        var parts = credential!.Split(':', 2);
        if (parts.Length != 2)
            return null;

        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{parts[0]}:{parts[1]}"));

        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await client.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
        return GetString(doc.RootElement, "access_token");
    }

    private async Task<JsonDocument> PostJsonAsync(string accessToken, string url, string json, CancellationToken ct)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
    }

    /// <summary>Digs the first capture/authorization id out of purchase_units[].payments.{collection}[].</summary>
    private static string? ExtractPaymentId(JsonElement root, string collection)
    {
        if (!root.TryGetProperty("purchase_units", out var units) || units.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var unit in units.EnumerateArray())
        {
            if (unit.TryGetProperty("payments", out var payments)
                && payments.TryGetProperty(collection, out var items)
                && items.ValueKind == JsonValueKind.Array
                && items.GetArrayLength() > 0)
            {
                return GetString(items[0], "id");
            }
        }
        return null;
    }

    private static string DescribeError(JsonElement root)
        => GetString(root, "message") ?? GetString(root, "name") ?? "order was not created";

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
