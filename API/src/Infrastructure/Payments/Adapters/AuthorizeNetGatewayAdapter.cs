using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Payments.Adapters;

/// <summary>
/// Authorize.Net gateway adapter (WO-33) using the JSON transaction API via <see cref="IHttpClientFactory"/>.
/// Credentials are stored in the Credential Vault (service type "authorizenet") as
/// <c>apiLoginId:transactionKey</c>; live/sandbox credentials are required. The client-side payment nonce
/// is passed as opaque data. <see cref="CaptureMode"/> selects authCapture vs. authOnly transactions.
/// </summary>
public class AuthorizeNetGatewayAdapter : PaymentGatewayAdapterBase
{
    private const string Endpoint = "https://api.authorize.net/xml/v1/request.api";

    public AuthorizeNetGatewayAdapter(ICredentialVault vault, IHttpClientFactory httpClientFactory, ILogger<AuthorizeNetGatewayAdapter> logger)
        : base(vault, httpClientFactory, logger) { }

    public override PaymentMethodType Method => PaymentMethodType.AuthorizeNet;

    public override Task<PaymentResult> AuthorizeAsync(PaymentRequest req, CaptureMode mode, CancellationToken ct)
        => GuardAsync("authorize", async () =>
        {
            var auth = await ResolveAuthAsync(ct);
            if (auth is null)
                return PaymentResult.Failed("Authorize.Net credentials are not configured in the Credential Vault (service type 'authorizenet', format 'apiLoginId:transactionKey').");

            var type = mode == CaptureMode.AuthorizeOnly ? "authOnlyTransaction" : "authCaptureTransaction";
            var transactionRequest = new Dictionary<string, object?>
            {
                ["transactionType"] = type,
                ["amount"] = Money(req.Amount),
                ["payment"] = new
                {
                    opaqueData = new { dataDescriptor = "COMMON.ACCEPT.INAPP.PAYMENT", dataValue = req.PaymentToken ?? string.Empty },
                },
            };

            using var doc = await SendAsync(auth.Value, transactionRequest, ct);
            return Map(doc.RootElement, type, req.Amount);
        });

    public override Task<PaymentResult> CaptureAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("capture", async () =>
        {
            var auth = await ResolveAuthAsync(ct);
            if (auth is null)
                return PaymentResult.Failed("Authorize.Net credentials are not configured in the Credential Vault (service type 'authorizenet').");

            var transId = payment.AuthorizationId ?? payment.GatewayReference;
            if (string.IsNullOrWhiteSpace(transId))
                return PaymentResult.Failed("Authorize.Net capture requires the original transaction id.");

            const string type = "priorAuthCaptureTransaction";
            var transactionRequest = new Dictionary<string, object?>
            {
                ["transactionType"] = type,
                ["amount"] = Money(amount),
                ["refTransId"] = transId,
            };

            using var doc = await SendAsync(auth.Value, transactionRequest, ct);
            return Map(doc.RootElement, type, amount);
        });

    public override Task<PaymentResult> RefundAsync(PaymentRecord payment, decimal amount, CancellationToken ct)
        => GuardAsync("refund", async () =>
        {
            var auth = await ResolveAuthAsync(ct);
            if (auth is null)
                return PaymentResult.Failed("Authorize.Net credentials are not configured in the Credential Vault (service type 'authorizenet').");

            var transId = payment.TransactionId ?? payment.AuthorizationId;
            if (string.IsNullOrWhiteSpace(transId))
                return PaymentResult.Failed("Authorize.Net refund requires the settled transaction id.");

            const string type = "refundTransaction";
            var transactionRequest = new Dictionary<string, object?>
            {
                ["transactionType"] = type,
                ["amount"] = Money(amount),
                // A live refund echoes the masked card of the original charge; stored masked PAN is used there.
                ["payment"] = new { creditCard = new { cardNumber = "XXXX", expirationDate = "XXXX" } },
                ["refTransId"] = transId,
            };

            using var doc = await SendAsync(auth.Value, transactionRequest, ct);
            return Map(doc.RootElement, type, amount);
        });

    private PaymentResult Map(JsonElement root, string transactionType, decimal amount)
    {
        if (!root.TryGetProperty("transactionResponse", out var txn) || txn.ValueKind != JsonValueKind.Object)
            return PaymentResult.Failed($"Authorize.Net: {DescribeError(root)}");

        var responseCode = GetString(txn, "responseCode");
        var transId = GetString(txn, "transId");

        if (responseCode != "1")
            return PaymentResult.Failed($"Authorize.Net declined (code {responseCode}): {DescribeTxnError(txn)}");

        return transactionType switch
        {
            "authOnlyTransaction" => PaymentResult.Authorized(transId, transId),
            "refundTransaction" => new PaymentResult(true, PaymentStatus.Refunded, TransactionId: transId, GatewayReference: transId, CapturedAmount: amount),
            _ => PaymentResult.Captured(transId, amount, transId, transId),
        };
    }

    private async Task<(string Name, string Key)?> ResolveAuthAsync(CancellationToken ct)
    {
        var credential = await ResolveCredentialAsync(ct);
        if (string.IsNullOrWhiteSpace(credential))
            return null;

        var parts = credential!.Split(':', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : null;
    }

    private async Task<JsonDocument> SendAsync((string Name, string Key) auth, IDictionary<string, object?> transactionRequest, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new
        {
            createTransactionRequest = new
            {
                merchantAuthentication = new { name = auth.Name, transactionKey = auth.Key },
                transactionRequest,
            },
        });

        var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        using var response = await client.SendAsync(request, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);
        // Authorize.Net JSON responses are prefixed with a UTF-8 BOM that breaks a strict parser.
        payload = payload.TrimStart('﻿', ' ', '\r', '\n', '\t');
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
    }

    private static string Money(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string DescribeTxnError(JsonElement txn)
    {
        if (txn.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            return GetString(errors[0], "errorText") ?? "declined";
        return "declined";
    }

    private static string DescribeError(JsonElement root)
    {
        if (root.TryGetProperty("messages", out var messages)
            && messages.TryGetProperty("message", out var list)
            && list.ValueKind == JsonValueKind.Array && list.GetArrayLength() > 0)
        {
            return GetString(list[0], "text") ?? "unknown error";
        }
        return "unexpected response";
    }

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
