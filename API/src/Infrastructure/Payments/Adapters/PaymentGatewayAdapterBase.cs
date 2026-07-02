using System.Net.Http;
using Microsoft.Extensions.Logging;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Payments.Adapters;

/// <summary>
/// Shared plumbing for the REST-based gateway adapters (WO-32/33): credential resolution from the
/// Credential Vault, a named <see cref="HttpClient"/> from the factory, and a common exception guard so
/// a provider/network failure always becomes a failed <see cref="PaymentResult"/> rather than throwing.
/// <para>
/// The adapters build structurally-correct requests for each provider's REST API. Live/sandbox API keys
/// must be configured in the Credential Vault for the calls to succeed against the real services.
/// </para>
/// </summary>
public abstract class PaymentGatewayAdapterBase : IPaymentGatewayAdapter
{
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);

    protected readonly ICredentialVault Vault;
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly ILogger Logger;

    protected PaymentGatewayAdapterBase(ICredentialVault vault, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        Vault = vault;
        HttpClientFactory = httpClientFactory;
        Logger = logger;
    }

    public abstract PaymentMethodType Method { get; }

    public abstract Task<PaymentResult> AuthorizeAsync(PaymentRequest req, CaptureMode mode, CancellationToken ct);
    public abstract Task<PaymentResult> CaptureAsync(PaymentRecord payment, decimal amount, CancellationToken ct);
    public abstract Task<PaymentResult> RefundAsync(PaymentRecord payment, decimal amount, CancellationToken ct);

    /// <summary>A named HttpClient (pooled by the factory) with a sane timeout for gateway calls.</summary>
    protected HttpClient CreateClient()
    {
        var client = HttpClientFactory.CreateClient($"payments:{Method}");
        client.Timeout = HttpTimeout;
        return client;
    }

    /// <summary>The raw credential string stored in the vault for this gateway, or <c>null</c> when absent.</summary>
    protected async Task<string?> ResolveCredentialAsync(CancellationToken ct)
    {
        var serviceType = PaymentGatewayDefaults.CredentialServiceType(Method);
        return serviceType is null ? null : await Vault.GetCredentialAsync(serviceType, ct);
    }

    /// <summary>
    /// Runs a gateway operation, converting any exception into a failed result. Callers keep the happy
    /// path; the guard centralizes the try/catch → <see cref="PaymentResult.Failed(string, PaymentStatus)"/>
    /// contract (adapters must never throw for a provider-side failure).
    /// </summary>
    protected async Task<PaymentResult> GuardAsync(string operation, Func<Task<PaymentResult>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "{Gateway} {Operation} failed.", Method, operation);
            return PaymentResult.Failed($"{Method} {operation} failed: {ex.Message}");
        }
    }

    /// <summary>Most gateways bill in the currency's minor unit (e.g. cents/paise). Structurally correct for the common case.</summary>
    protected static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
