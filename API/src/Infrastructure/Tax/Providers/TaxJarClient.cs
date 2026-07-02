using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Tax.Providers;

/// <summary>
/// TaxJar provider client (WO-36). Calculates tax via <c>POST /v2/taxes</c> and reports finalized orders
/// via <c>POST /v2/transactions/orders</c> for remittance. Authenticates with a Bearer API token resolved
/// at runtime from the Credential Vault under the key <c>"taxjar"</c>.
/// <para>
/// Live TaxJar credentials are required — without them, and on any HTTP failure, this client throws so
/// <see cref="ITaxCalculationService"/> applies its flat-rate fallback (AC-TAX-001.4 / AC-TAX-002.3).
/// </para>
/// </summary>
public class TaxJarClient : ITaxProviderClient
{
    private const string CredentialKey = "taxjar";
    private const string BaseUrl = "https://api.taxjar.com";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialVault _vault;

    public TaxJarClient(IHttpClientFactory httpClientFactory, ICredentialVault vault)
    {
        _httpClientFactory = httpClientFactory;
        _vault = vault;
    }

    public TaxProviderType Provider => TaxProviderType.TaxJar;

    public async Task<TaxBreakdown> CalculateAsync(TaxCalculationRequest req, CancellationToken ct)
    {
        var client = await CreateClientAsync(ct);

        var payload = new
        {
            from_country = req.Origin.CountryCode,
            from_zip = req.Origin.PostalCode,
            from_state = req.Origin.Region,
            from_city = req.Origin.City,
            to_country = req.Destination.CountryCode,
            to_zip = req.Destination.PostalCode,
            to_state = req.Destination.Region,
            to_city = req.Destination.City,
            amount = req.Lines.Sum(l => l.Amount * l.Quantity),
            shipping = req.ShippingAmount,
            line_items = req.Lines.Select((l, i) => new
            {
                id = (i + 1).ToString(CultureInfo.InvariantCulture),
                quantity = l.Quantity,
                product_tax_code = l.TaxCategoryCode,
                unit_price = l.Amount,
            }).ToArray(),
        };

        using var response = await client.PostAsJsonAsync($"{BaseUrl}/v2/taxes", payload, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return MapResponse(document.RootElement);
    }

    public async Task ReportTransactionAsync(Guid orderId, CancellationToken ct)
    {
        // Records the finalized order with TaxJar for reporting/remittance. A production implementation
        // loads the order's amounts, line items and addresses and maps them onto this payload — order
        // persistence is owned by Order Management, so this client only owns the TaxJar call shape.
        // Live TaxJar credentials are required; CreateClientAsync throws when they are missing.
        var client = await CreateClientAsync(ct);

        var payload = new
        {
            transaction_id = orderId.ToString(),
            transaction_date = DateTime.UtcNow.ToString("O"),
        };

        using var response = await client.PostAsJsonAsync($"{BaseUrl}/v2/transactions/orders", payload, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpClient> CreateClientAsync(CancellationToken ct)
    {
        // Live TaxJar API token (Bearer), stored encrypted in the Credential Vault under "taxjar".
        var token = await _vault.GetCredentialAsync(CredentialKey, ct);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("TaxJar credentials are not configured (Credential Vault key 'taxjar').");

        var client = _httpClientFactory.CreateClient("taxjar");
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Maps a TaxJar <c>/v2/taxes</c> response onto a <see cref="TaxBreakdown"/>. Shape:
    /// <c>{ "tax": { "amount_to_collect": 1.23, "rate": 0.0825, "breakdown": { ...*_tax_collectable, *_tax_rate } } }</c>.
    /// </summary>
    private static TaxBreakdown MapResponse(JsonElement root)
    {
        if (!root.TryGetProperty("tax", out var tax) || tax.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("TaxJar response did not contain a 'tax' object.");

        var totalTax = tax.TryGetProperty("amount_to_collect", out var amt) && amt.TryGetDecimal(out var a) ? a : 0m;

        var jurisdictions = new List<TaxJurisdiction>();
        if (tax.TryGetProperty("breakdown", out var breakdown) && breakdown.ValueKind == JsonValueKind.Object)
        {
            AddJurisdiction(jurisdictions, breakdown, "country_tax_collectable", "country_tax_rate", "Country", "Country");
            AddJurisdiction(jurisdictions, breakdown, "state_tax_collectable", "state_tax_rate", "State", "State");
            AddJurisdiction(jurisdictions, breakdown, "county_tax_collectable", "county_tax_rate", "County", "County");
            AddJurisdiction(jurisdictions, breakdown, "city_tax_collectable", "city_tax_rate", "City", "City");
            AddJurisdiction(jurisdictions, breakdown, "special_district_tax_collectable", "special_tax_rate", "Special District", "Special");
        }

        // No itemized breakdown but tax is owed: synthesize a single total jurisdiction.
        if (jurisdictions.Count == 0 && totalTax > 0m)
        {
            var rate = tax.TryGetProperty("rate", out var r) && r.TryGetDecimal(out var rv) ? rv : 0m;
            jurisdictions.Add(new TaxJurisdiction("TaxJar", "Total", rate, totalTax));
        }

        return new TaxBreakdown(totalTax, jurisdictions, FallbackApplied: false);
    }

    private static void AddJurisdiction(
        List<TaxJurisdiction> list, JsonElement breakdown, string amountProp, string rateProp, string name, string type)
    {
        var amount = breakdown.TryGetProperty(amountProp, out var a) && a.TryGetDecimal(out var av) ? av : 0m;
        if (amount <= 0m)
            return;

        var rate = breakdown.TryGetProperty(rateProp, out var r) && r.TryGetDecimal(out var rv) ? rv : 0m;
        list.Add(new TaxJurisdiction(name, type, rate, amount));
    }
}
