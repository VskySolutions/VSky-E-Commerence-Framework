using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;

namespace VSky.Infrastructure.Shipping.Carriers;

/// <summary>
/// UPS rating adapter (WO-40), built structurally against the UPS Rating REST API (/api/rating). The
/// credential is resolved from the credential vault under the "ups" service-type key and is expected to
/// be a JSON document: { "accessToken", "accountNumber", "baseUrl" (optional) }; a bare string is
/// treated as an OAuth access token. When no credential is configured — or the live call fails for any
/// reason — an empty list is returned so the aggregating service simply excludes UPS (AC-SHP-001.3).
/// </summary>
/// <remarks>
/// This performs the real UPS Rate request/response mapping, but returning actual quotes requires a
/// valid UPS OAuth access token (and account number) plus network access to a live UPS endpoint.
/// </remarks>
public class UpsCarrierClient : ICarrierClient
{
    private const string CredentialKey = "ups";
    private const string DefaultBaseUrl = "https://onlinetools.ups.com";
    private const string RatingPath = "/api/rating/v2409/Rate";
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialVault _vault;

    public UpsCarrierClient(IHttpClientFactory httpClientFactory, ICredentialVault vault)
    {
        _httpClientFactory = httpClientFactory;
        _vault = vault;
    }

    public string CarrierName => "UPS";

    public async Task<IReadOnlyList<ShippingRateOption>> GetRatesAsync(CarrierRateRequest request, CancellationToken ct)
    {
        var raw = await _vault.GetCredentialAsync(CredentialKey, ct);
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<ShippingRateOption>();

        var credential = UpsCredential.Parse(raw);
        if (string.IsNullOrWhiteSpace(credential.AccessToken))
            return Array.Empty<ShippingRateOption>();

        try
        {
            var client = _httpClientFactory.CreateClient("ups");
            client.Timeout = HttpTimeout;

            var baseUrl = string.IsNullOrWhiteSpace(credential.BaseUrl) ? DefaultBaseUrl : credential.BaseUrl!.TrimEnd('/');
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{RatingPath}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credential.AccessToken);

            var payload = BuildRequestBody(request, credential);
            var body = JsonSerializer.Serialize(payload, payload.GetType());
            httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<ShippingRateOption>();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return MapResponse(document.RootElement);
        }
        catch
        {
            // Network / auth / parse failures exclude UPS from the aggregate rather than fail the quote.
            return Array.Empty<ShippingRateOption>();
        }
    }

    private static object BuildRequestBody(CarrierRateRequest request, UpsCredential credential) => new
    {
        RateRequest = new
        {
            Request = new { RequestOption = "Shop" },
            Shipment = new
            {
                Shipper = new
                {
                    ShipperNumber = credential.AccountNumber ?? string.Empty,
                    Address = BuildAddress(request.Origin),
                },
                ShipFrom = new { Address = BuildAddress(request.Origin) },
                ShipTo = new { Address = BuildAddress(request.Destination) },
                Package = new
                {
                    PackagingType = new { Code = "02" },
                    PackageWeight = new
                    {
                        UnitOfMeasurement = new { Code = "KGS" },
                        Weight = request.WeightKg.ToString(CultureInfo.InvariantCulture),
                    },
                },
            },
        },
    };

    private static object BuildAddress(CarrierAddress address) => new
    {
        PostalCode = address.PostalCode ?? string.Empty,
        StateProvinceCode = address.Region ?? string.Empty,
        CountryCode = address.CountryCode ?? string.Empty,
    };

    private static IReadOnlyList<ShippingRateOption> MapResponse(JsonElement root)
    {
        var options = new List<ShippingRateOption>();
        if (!root.TryGetProperty("RateResponse", out var rateResponse) ||
            !rateResponse.TryGetProperty("RatedShipment", out var rated))
            return options;

        // UPS returns RatedShipment as an object for a single service or an array for several.
        if (rated.ValueKind == JsonValueKind.Array)
        {
            foreach (var shipment in rated.EnumerateArray())
                AddOption(shipment, options);
        }
        else if (rated.ValueKind == JsonValueKind.Object)
        {
            AddOption(rated, options);
        }

        return options;
    }

    private static void AddOption(JsonElement shipment, List<ShippingRateOption> options)
    {
        string? serviceCode = null;
        if (shipment.TryGetProperty("Service", out var service) &&
            service.TryGetProperty("Code", out var code))
            serviceCode = code.GetString();

        decimal? rate = null;
        if (shipment.TryGetProperty("TotalCharges", out var charges) &&
            charges.TryGetProperty("MonetaryValue", out var monetary) &&
            monetary.ValueKind == JsonValueKind.String &&
            decimal.TryParse(monetary.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            rate = value;

        int? transitDays = null;
        if (shipment.TryGetProperty("GuaranteedDelivery", out var guaranteed) &&
            guaranteed.TryGetProperty("BusinessDaysInTransit", out var transit) &&
            transit.ValueKind == JsonValueKind.String &&
            int.TryParse(transit.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var days))
            transitDays = days;

        if (rate is null)
            return;

        options.Add(new ShippingRateOption(
            MethodId: serviceCode ?? "UPS",
            Name: serviceCode is null ? "UPS" : $"UPS {serviceCode}",
            Carrier: "UPS",
            EstimatedDeliveryDays: transitDays,
            Rate: rate.Value));
    }

    private sealed record UpsCredential(string? AccessToken, string? AccountNumber, string? BaseUrl)
    {
        public static UpsCredential Parse(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                return new UpsCredential(
                    GetString(root, "accessToken") ?? GetString(root, "token"),
                    GetString(root, "accountNumber"),
                    GetString(root, "baseUrl"));
            }
            catch (JsonException)
            {
                // Not JSON: treat the whole credential value as the OAuth access token.
                return new UpsCredential(raw, null, null);
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
