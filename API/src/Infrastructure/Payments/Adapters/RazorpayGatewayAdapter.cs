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
/// Razorpay gateway adapter (WO-33) over the REST API via <see cref="IHttpClientFactory"/>. Credentials
/// live in the Credential Vault (service type "razorpay") as <c>key_id:key_secret</c> and are sent as
/// HTTP Basic auth; live/test keys are required. Amounts are billed in the minor unit (paise).
/// <see cref="CaptureMode"/> maps to the order <c>payment_capture</c> flag / an explicit capture call.
/// </summary>
public class RazorpayGatewayAdapter : PaymentGatewayAdapterBase
{
    public RazorpayGatewayAdapter(ICredentialVault vault, IHttpClientFactory httpClientFactory, ILogger<RazorpayGatewayAdapter> logger)
        : base(vault, httpClientFactory, logger) { }

    public override PaymentMethodType Method => PaymentMethodType.Razorpay;

    public override Task<PaymentResult> AuthorizeAsync(PaymentRequest req, CaptureMode mode, CancellationToken ct)
        => GuardAsync("authorize", async () =>
        {
            var auth = await GetAuthAsync(ct);
            if (auth is null)
                return PaymentResult.Failed("Razorpay keys are not configured in the Credential Vault (service type 'razorpay', format 'key_id:key_secret').");
            var (basic, baseUrl) = auth.Value;

            // If the client has already produced a payment_id, capture/authorize it directly.
            if (!string.IsNullOrWhiteSpace(req.PaymentToken))
            {
                if (mode == CaptureMode.AuthorizeAndCapture)
                {
                    var body = JsonSerializer.Serialize(new { amount = ToMinorUnits(req.Amount), currency = req.CurrencyCode });
                    using var capDoc = await PostJsonAsync(basic, $"{baseUrl}/payments/{req.PaymentToken}/capture", body, ct);
                    return MapPayment(capDoc.RootElement, req.Amount, req.PaymentToken!);
                }

                // Authorize-only: leave the payment authorized for a later capture.
                return PaymentResult.Authorized(req.PaymentToken, req.PaymentToken);
            }

            // Otherwise create an order the client-side checkout will complete.
            var orderBody = JsonSerializer.Serialize(new
            {
                amount = ToMinorUnits(req.Amount),
                currency = req.CurrencyCode,
                payment_capture = mode == CaptureMode.AuthorizeAndCapture,
            });
            using var doc = await PostJsonAsync(basic, $"{baseUrl}/orders", orderBody, ct);
            var root = doc.RootElement;
            var orderId = GetString(root, "id");

            if (string.IsNullOrWhiteSpace(orderId))
                return PaymentResult.Failed($"Razorpay: {DescribeError(root)}");

            return new PaymentResult(true, PaymentStatus.Pending, AuthorizationId: orderId, GatewayReference: orderId);
        });

    public override Task<PaymentResult> CaptureAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("capture", async () =>
        {
            var auth = await GetAuthAsync(ct);
            if (auth is null)
                return PaymentResult.Failed("Razorpay keys are not configured in the Credential Vault (service type 'razorpay').");
            var (basic, baseUrl) = auth.Value;

            var paymentId = payment.AuthorizationId ?? payment.GatewayReference;
            if (string.IsNullOrWhiteSpace(paymentId))
                return PaymentResult.Failed("Razorpay capture requires the payment id.");

            var body = JsonSerializer.Serialize(new { amount = ToMinorUnits(amount), currency = payment.CurrencyCode });
            using var doc = await PostJsonAsync(basic, $"{baseUrl}/payments/{paymentId}/capture", body, ct);
            return MapPayment(doc.RootElement, amount, paymentId!);
        });

    public override Task<PaymentResult> RefundAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("refund", async () =>
        {
            var auth = await GetAuthAsync(ct);
            if (auth is null)
                return PaymentResult.Failed("Razorpay keys are not configured in the Credential Vault (service type 'razorpay').");
            var (basic, baseUrl) = auth.Value;

            var paymentId = payment.TransactionId ?? payment.AuthorizationId ?? payment.GatewayReference;
            if (string.IsNullOrWhiteSpace(paymentId))
                return PaymentResult.Failed("Razorpay refund requires the payment id.");

            var body = JsonSerializer.Serialize(new { amount = ToMinorUnits(amount) });
            using var doc = await PostJsonAsync(basic, $"{baseUrl}/payments/{paymentId}/refund", body, ct);
            var root = doc.RootElement;
            var refundId = GetString(root, "id");

            return refundId is not null
                ? new PaymentResult(true, PaymentStatus.Refunded, TransactionId: refundId, GatewayReference: refundId, CapturedAmount: amount)
                : PaymentResult.Failed($"Razorpay refund failed: {DescribeError(root)}");
        });

    private static PaymentResult MapPayment(JsonElement root, decimal amount, string paymentId)
    {
        var status = GetString(root, "status");
        return status switch
        {
            "captured" => PaymentResult.Captured(paymentId, amount, paymentId, paymentId),
            "authorized" => PaymentResult.Authorized(paymentId, paymentId),
            _ => PaymentResult.Failed($"Razorpay payment status '{status ?? DescribeError(root)}'."),
        };
    }

    /// <summary>Basic-auth header value plus the endpoint this credential belongs to; null when unconfigured.</summary>
    private async Task<(string Basic, string BaseUrl)?> GetAuthAsync(CancellationToken ct)
    {
        var resolved = await ResolveAsync(ct);
        if (resolved is null || !resolved.Value.Contains(':'))
            return null;
        return (Convert.ToBase64String(Encoding.UTF8.GetBytes(resolved.Value)), RequireBaseUrl(resolved));
    }

    private async Task<JsonDocument> PostJsonAsync(string basic, string url, string json, CancellationToken ct)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var response = await client.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
    }

    private static string DescribeError(JsonElement root)
        => root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object
            ? GetString(error, "description") ?? "unknown error"
            : "unexpected response";

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
