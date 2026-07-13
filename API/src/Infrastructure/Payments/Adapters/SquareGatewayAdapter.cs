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
/// Square gateway adapter (WO-33) over the Payments REST API via <see cref="IHttpClientFactory"/>. The
/// access token is stored in Credentials_Square; the credential's <c>IsProduction</c> flag selects the
/// live vs. sandbox endpoint. The card nonce/source id is supplied as the request's
/// <see cref="PaymentRequest.PaymentToken"/>. <see cref="CaptureMode"/> maps to the CreatePayment
/// <c>autocomplete</c> flag.
/// </summary>
public class SquareGatewayAdapter : PaymentGatewayAdapterBase
{
    private const string LiveBaseUrl = "https://connect.squareup.com/v2";
    private const string SandboxBaseUrl = "https://connect.squareupsandbox.com/v2";
    private const string SquareVersion = "2024-01-18";

    public SquareGatewayAdapter(ICredentialVault vault, IHttpClientFactory httpClientFactory, ILogger<SquareGatewayAdapter> logger)
        : base(vault, httpClientFactory, logger) { }

    public override PaymentMethodType Method => PaymentMethodType.Square;

    public override Task<PaymentResult> AuthorizeAsync(PaymentRequest req, CaptureMode mode, CancellationToken ct)
        => GuardAsync("authorize", async () =>
        {
            var ctx = await ResolveContextAsync(ct);
            if (ctx is null)
                return PaymentResult.Failed("Square access token is not configured (Credentials_Square).");
            var (baseUrl, token) = ctx.Value;
            if (string.IsNullOrWhiteSpace(req.PaymentToken))
                return PaymentResult.Failed("Square requires a card nonce / source id (PaymentToken).");

            var body = JsonSerializer.Serialize(new
            {
                source_id = req.PaymentToken,
                idempotency_key = Guid.NewGuid().ToString("N"),
                amount_money = new { amount = ToMinorUnits(req.Amount), currency = req.CurrencyCode },
                autocomplete = mode == CaptureMode.AuthorizeAndCapture,
            });

            using var doc = await PostJsonAsync(token, $"{baseUrl}/payments", body, ct);
            var root = doc.RootElement;
            if (!root.TryGetProperty("payment", out var payment))
                return PaymentResult.Failed($"Square: {DescribeError(root)}");

            var id = GetString(payment, "id");
            var status = GetString(payment, "status");
            return status switch
            {
                "COMPLETED" => PaymentResult.Captured(id, req.Amount, id, id),
                "APPROVED" => PaymentResult.Authorized(id, id),
                _ => PaymentResult.Failed($"Square payment status '{status}'."),
            };
        });

    public override Task<PaymentResult> CaptureAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("capture", async () =>
        {
            var ctx = await ResolveContextAsync(ct);
            if (ctx is null)
                return PaymentResult.Failed("Square access token is not configured (Credentials_Square).");
            var (baseUrl, token) = ctx.Value;

            var paymentId = payment.AuthorizationId ?? payment.GatewayReference;
            if (string.IsNullOrWhiteSpace(paymentId))
                return PaymentResult.Failed("Square capture requires the original payment id.");

            using var doc = await PostJsonAsync(token, $"{baseUrl}/payments/{paymentId}/complete", "{}", ct);
            var root = doc.RootElement;
            if (!root.TryGetProperty("payment", out var completed))
                return PaymentResult.Failed($"Square: {DescribeError(root)}");

            return string.Equals(GetString(completed, "status"), "COMPLETED", StringComparison.OrdinalIgnoreCase)
                ? PaymentResult.Captured(paymentId, amount, paymentId, paymentId)
                : PaymentResult.Failed($"Square capture returned status '{GetString(completed, "status")}'.");
        });

    public override Task<PaymentResult> RefundAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("refund", async () =>
        {
            var ctx = await ResolveContextAsync(ct);
            if (ctx is null)
                return PaymentResult.Failed("Square access token is not configured (Credentials_Square).");
            var (baseUrl, token) = ctx.Value;

            var paymentId = payment.TransactionId ?? payment.AuthorizationId ?? payment.GatewayReference;
            if (string.IsNullOrWhiteSpace(paymentId))
                return PaymentResult.Failed("Square refund requires the original payment id.");

            var body = JsonSerializer.Serialize(new
            {
                idempotency_key = Guid.NewGuid().ToString("N"),
                payment_id = paymentId,
                amount_money = new { amount = ToMinorUnits(amount), currency = payment.CurrencyCode },
            });

            using var doc = await PostJsonAsync(token, $"{baseUrl}/refunds", body, ct);
            var root = doc.RootElement;
            if (!root.TryGetProperty("refund", out var refund))
                return PaymentResult.Failed($"Square: {DescribeError(root)}");

            var status = GetString(refund, "status");
            return status is "PENDING" or "COMPLETED"
                ? new PaymentResult(true, PaymentStatus.Refunded, TransactionId: GetString(refund, "id"), GatewayReference: GetString(refund, "id"), CapturedAmount: amount)
                : PaymentResult.Failed($"Square refund returned status '{status}'.");
        });

    /// <summary>Resolves the active Square access token and picks the live/sandbox base URL from its production flag.</summary>
    private async Task<(string BaseUrl, string Token)?> ResolveContextAsync(CancellationToken ct)
    {
        var resolved = await ResolveAsync(ct);
        if (resolved is null)
            return null;
        var baseUrl = resolved.IsProduction ? LiveBaseUrl : SandboxBaseUrl;
        return (baseUrl, resolved.Value);
    }

    private async Task<JsonDocument> PostJsonAsync(string accessToken, string url, string json, CancellationToken ct)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Square-Version", SquareVersion);

        using var response = await client.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
    }

    private static string DescribeError(JsonElement root)
    {
        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            return GetString(errors[0], "detail") ?? GetString(errors[0], "code") ?? "unknown error";
        return "unexpected response";
    }

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
