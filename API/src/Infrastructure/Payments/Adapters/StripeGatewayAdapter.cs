using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Payments.Adapters;

/// <summary>
/// Stripe gateway adapter (WO-32) using the PaymentIntents REST API over <see cref="IHttpClientFactory"/>.
/// The secret key is resolved from the Credential Vault (service type "stripe"); a live/sandbox key is
/// required for the calls to succeed. <see cref="CaptureMode"/> maps to the PaymentIntent
/// <c>capture_method</c> (automatic vs. manual).
/// </summary>
public class StripeGatewayAdapter : PaymentGatewayAdapterBase
{
    private const string BaseUrl = "https://api.stripe.com/v1";

    public StripeGatewayAdapter(ICredentialVault vault, IHttpClientFactory httpClientFactory, ILogger<StripeGatewayAdapter> logger)
        : base(vault, httpClientFactory, logger) { }

    public override PaymentMethodType Method => PaymentMethodType.Stripe;

    public override Task<PaymentResult> AuthorizeAsync(PaymentRequest req, CaptureMode mode, CancellationToken ct)
        => GuardAsync("authorize", async () =>
        {
            var secretKey = await ResolveCredentialAsync(ct);
            if (string.IsNullOrWhiteSpace(secretKey))
                return PaymentResult.Failed("Stripe secret key is not configured in the Credential Vault (service type 'stripe').");

            var form = new Dictionary<string, string>
            {
                ["amount"] = ToMinorUnits(req.Amount).ToString(),
                ["currency"] = req.CurrencyCode.ToLowerInvariant(),
                ["confirm"] = "true",
                // AuthorizeOnly holds funds (manual capture); AuthorizeAndCapture settles immediately.
                ["capture_method"] = mode == CaptureMode.AuthorizeOnly ? "manual" : "automatic",
            };
            if (!string.IsNullOrWhiteSpace(req.PaymentToken))
                form["payment_method"] = req.PaymentToken!;
            if (!string.IsNullOrWhiteSpace(req.ReturnUrl))
                form["return_url"] = req.ReturnUrl!;

            using var doc = await PostFormAsync(secretKey!, $"{BaseUrl}/payment_intents", form, ct);
            var root = doc.RootElement;

            if (TryGetError(root, out var error))
                return PaymentResult.Failed($"Stripe: {error}");

            var id = GetString(root, "id");
            var status = GetString(root, "status");
            var charge = GetString(root, "latest_charge");

            return status switch
            {
                "succeeded" => PaymentResult.Captured(charge ?? id, req.Amount, id, id),
                "requires_capture" => PaymentResult.Authorized(id, id),
                _ => PaymentResult.Failed($"Stripe PaymentIntent status '{status}' is not a completed authorization."),
            };
        });

    public override Task<PaymentResult> CaptureAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("capture", async () =>
        {
            var secretKey = await ResolveCredentialAsync(ct);
            if (string.IsNullOrWhiteSpace(secretKey))
                return PaymentResult.Failed("Stripe secret key is not configured in the Credential Vault (service type 'stripe').");

            var intentId = payment.AuthorizationId ?? payment.GatewayReference;
            if (string.IsNullOrWhiteSpace(intentId))
                return PaymentResult.Failed("Stripe capture requires the original PaymentIntent id.");

            var form = new Dictionary<string, string> { ["amount_to_capture"] = ToMinorUnits(amount).ToString() };

            using var doc = await PostFormAsync(secretKey!, $"{BaseUrl}/payment_intents/{intentId}/capture", form, ct);
            var root = doc.RootElement;

            if (TryGetError(root, out var error))
                return PaymentResult.Failed($"Stripe: {error}");

            var status = GetString(root, "status");
            return status == "succeeded"
                ? PaymentResult.Captured(GetString(root, "latest_charge") ?? intentId, amount, intentId, intentId)
                : PaymentResult.Failed($"Stripe capture returned status '{status}'.");
        });

    public override Task<PaymentResult> RefundAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("refund", async () =>
        {
            var secretKey = await ResolveCredentialAsync(ct);
            if (string.IsNullOrWhiteSpace(secretKey))
                return PaymentResult.Failed("Stripe secret key is not configured in the Credential Vault (service type 'stripe').");

            var intentId = payment.AuthorizationId ?? payment.GatewayReference;
            if (string.IsNullOrWhiteSpace(intentId))
                return PaymentResult.Failed("Stripe refund requires the original PaymentIntent id.");

            var form = new Dictionary<string, string>
            {
                ["payment_intent"] = intentId!,
                ["amount"] = ToMinorUnits(amount).ToString(),
            };

            using var doc = await PostFormAsync(secretKey!, $"{BaseUrl}/refunds", form, ct);
            var root = doc.RootElement;

            if (TryGetError(root, out var error))
                return PaymentResult.Failed($"Stripe: {error}");

            var status = GetString(root, "status");
            return status is "succeeded" or "pending"
                ? new PaymentResult(true, PaymentStatus.Refunded, TransactionId: GetString(root, "id"), GatewayReference: GetString(root, "id"), CapturedAmount: amount)
                : PaymentResult.Failed($"Stripe refund returned status '{status}'.");
        });

    private async Task<JsonDocument> PostFormAsync(string secretKey, string url, IDictionary<string, string> form, CancellationToken ct)
    {
        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        using var response = await client.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
    }

    private static bool TryGetError(JsonElement root, out string message)
    {
        if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
        {
            message = GetString(error, "message") ?? "unknown error";
            return true;
        }
        message = string.Empty;
        return false;
    }

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
