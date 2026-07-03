using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;

namespace VSky.Infrastructure.Shipping.Carriers;

/// <summary>
/// USPS rating adapter (WO-41), built structurally against the modern USPS REST APIs (apis.usps.com).
/// The credential is resolved from the credential vault under the "usps" service-type key and is expected
/// to be a JSON document: { "consumerKey", "consumerSecret", "baseUrl" (optional) }. USPS uses OAuth2
/// client-credentials, so this exchanges the consumer key/secret for a bearer token, then calls the
/// base-rates search endpoint. When no credential is configured — or any call fails — an empty list is
/// returned so the aggregating service simply excludes USPS (AC-SHP-001.3).
/// </summary>
/// <remarks>
/// The request/response mapping is real, but returning live quotes requires valid USPS API credentials
/// and network access to a live USPS endpoint.
/// </remarks>
public class UspsCarrierClient : ICarrierClient
{
    private const string CredentialKey = "usps";
    private const string DefaultBaseUrl = "https://apis.usps.com";
    private const string TokenPath = "/oauth2/v3/token";
    private const string RatePath = "/prices/v3/base-rates/search";
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialVault _vault;

    public UspsCarrierClient(IHttpClientFactory httpClientFactory, ICredentialVault vault)
    {
        _httpClientFactory = httpClientFactory;
        _vault = vault;
    }

    public string CarrierName => "USPS";

    public async Task<IReadOnlyList<ShippingRateOption>> GetRatesAsync(CarrierRateRequest request, CancellationToken ct)
    {
        var raw = await _vault.GetCredentialAsync(CredentialKey, ct);
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<ShippingRateOption>();

        var credential = UspsCredential.Parse(raw);
        if (string.IsNullOrWhiteSpace(credential.ConsumerKey) || string.IsNullOrWhiteSpace(credential.ConsumerSecret))
            return Array.Empty<ShippingRateOption>();

        // USPS domestic rating is US-origin/US-destination only; skip non-US shipments cleanly.
        if (!IsUnitedStates(request.Origin) || !IsUnitedStates(request.Destination))
            return Array.Empty<ShippingRateOption>();

        try
        {
            var client = _httpClientFactory.CreateClient("usps");
            client.Timeout = HttpTimeout;
            var baseUrl = string.IsNullOrWhiteSpace(credential.BaseUrl) ? DefaultBaseUrl : credential.BaseUrl!.TrimEnd('/');

            var token = await GetAccessTokenAsync(client, baseUrl, credential, ct);
            if (string.IsNullOrWhiteSpace(token))
                return Array.Empty<ShippingRateOption>();

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{RatePath}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = BuildRequestBody(request);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload, payload.GetType()), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<ShippingRateOption>();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return MapResponse(document.RootElement);
        }
        catch
        {
            return Array.Empty<ShippingRateOption>();
        }
    }

    private static async Task<string?> GetAccessTokenAsync(HttpClient client, string baseUrl, UspsCredential credential, CancellationToken ct)
    {
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{TokenPath}")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = credential.ConsumerKey!,
                ["client_secret"] = credential.ConsumerSecret!,
            }),
        };

        using var tokenResponse = await client.SendAsync(tokenRequest, ct);
        if (!tokenResponse.IsSuccessStatusCode)
            return null;

        await using var stream = await tokenResponse.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("access_token", out var token) ? token.GetString() : null;
    }

    // Pounds are the USPS unit; convert from the request's kilograms.
    private static object BuildRequestBody(CarrierRateRequest request) => new
    {
        originZIPCode = request.Origin.PostalCode ?? string.Empty,
        destinationZIPCode = request.Destination.PostalCode ?? string.Empty,
        weight = Math.Round(request.WeightKg * 2.20462m, 2),
        mailClass = "USPS_GROUND_ADVANTAGE",
        priceType = "RETAIL",
    };

    private static IReadOnlyList<ShippingRateOption> MapResponse(JsonElement root)
    {
        var options = new List<ShippingRateOption>();

        // A base-rates search returns either a single rate object or a { rates: [...] } collection.
        if (root.TryGetProperty("rates", out var rates) && rates.ValueKind == JsonValueKind.Array)
        {
            foreach (var rate in rates.EnumerateArray())
                AddOption(rate, options);
        }
        else
        {
            AddOption(root, options);
        }

        return options;
    }

    private static void AddOption(JsonElement element, List<ShippingRateOption> options)
    {
        var mailClass = element.TryGetProperty("mailClass", out var mc) ? mc.GetString() : null;

        decimal? price = null;
        if (element.TryGetProperty("totalBasePrice", out var tbp) && tbp.TryGetDecimal(out var t))
            price = t;
        else if (element.TryGetProperty("price", out var p) && p.TryGetDecimal(out var pv))
            price = pv;

        if (price is null)
            return;

        options.Add(new ShippingRateOption(
            MethodId: mailClass ?? "USPS",
            Name: mailClass is null ? "USPS" : $"USPS {mailClass.Replace('_', ' ')}",
            Carrier: "USPS",
            EstimatedDeliveryDays: null,
            Rate: price.Value));
    }

    private static bool IsUnitedStates(CarrierAddress address)
        => string.IsNullOrWhiteSpace(address.CountryCode)
           || address.CountryCode!.Equals("US", StringComparison.OrdinalIgnoreCase)
           || address.CountryCode!.Equals("USA", StringComparison.OrdinalIgnoreCase);

    private sealed record UspsCredential(string? ConsumerKey, string? ConsumerSecret, string? BaseUrl)
    {
        public static UspsCredential Parse(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                return new UspsCredential(
                    GetString(root, "consumerKey") ?? GetString(root, "clientId"),
                    GetString(root, "consumerSecret") ?? GetString(root, "clientSecret"),
                    GetString(root, "baseUrl"));
            }
            catch (JsonException)
            {
                return new UspsCredential(null, null, null);
            }
        }

        private static string? GetString(JsonElement root, string name)
            => root.ValueKind == JsonValueKind.Object &&
               root.TryGetProperty(name, out var el) &&
               el.ValueKind == JsonValueKind.String
                ? el.GetString()
                : null;
    }
}
