using System.Net.Http;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;

namespace VSky.Infrastructure.Credentials;

/// <summary>
/// Lightweight reachability probe per service type. Full provider-specific authentication tests are
/// added alongside each integration client (out of scope for this work order).
/// </summary>
public class CredentialConnectivityChecker : ICredentialConnectivityChecker
{
    private static readonly IReadOnlyDictionary<string, string> ProbeEndpoints =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["stripe"] = "https://api.stripe.com",
            ["stripe-tax"] = "https://api.stripe.com",
            ["paypal"] = "https://api-m.paypal.com",
            ["razorpay"] = "https://api.razorpay.com",
            ["square"] = "https://connect.squareup.com",
            ["authorizenet"] = "https://api.authorize.net",
            ["taxjar"] = "https://api.taxjar.com",
            ["dhl"] = "https://api-eu.dhl.com",
            ["ups"] = "https://onlinetools.ups.com",
            ["fedex"] = "https://apis.fedex.com",
            ["usps"] = "https://secure.shippingapis.com",
        };

    private readonly IHttpClientFactory _httpClientFactory;

    public CredentialConnectivityChecker(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;

    public async Task<ConnectivityTestResult> TestAsync(string serviceType, string plaintextValue, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(plaintextValue))
            return new ConnectivityTestResult(false, "No credential value is configured.", now);

        if (!ProbeEndpoints.TryGetValue(serviceType, out var url))
            return new ConnectivityTestResult(true,
                $"Credential stored. No automated connectivity probe is defined for '{serviceType}'.", now);

        try
        {
            var client = _httpClientFactory.CreateClient("credential-probe");
            client.Timeout = TimeSpan.FromSeconds(8);

            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return new ConnectivityTestResult(true, $"Reached {url} (HTTP {(int)response.StatusCode}).", now);
        }
        catch (Exception ex)
        {
            return new ConnectivityTestResult(false, $"Could not reach the provider endpoint: {ex.Message}", now);
        }
    }
}
