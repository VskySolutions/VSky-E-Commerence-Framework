using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Tax.Providers;

/// <summary>
/// Stripe Tax provider client (WO-36). Calculates tax via <c>POST /v1/tax/calculations</c>. Stripe's API
/// is form-encoded and works in the currency's smallest unit (e.g. cents). Authenticates with a Bearer
/// secret key resolved at runtime from the Credential Vault under the key <c>"stripe-tax"</c>.
/// <para>
/// Live Stripe credentials are required — without them, and on any HTTP failure, this client throws so
/// <see cref="ITaxCalculationService"/> applies its flat-rate fallback (AC-TAX-001.4 / AC-TAX-002.3).
/// Transaction reporting is a no-op: only TaxJar performs remittance reporting.
/// </para>
/// </summary>
public class StripeTaxClient : ITaxProviderClient
{
    private const string CredentialKey = "stripe-tax";
    private const string CalculationsUrl = "https://api.stripe.com/v1/tax/calculations";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialVault _vault;

    public StripeTaxClient(IHttpClientFactory httpClientFactory, ICredentialVault vault)
    {
        _httpClientFactory = httpClientFactory;
        _vault = vault;
    }

    public TaxProviderType Provider => TaxProviderType.StripeTax;

    public async Task<TaxBreakdown> CalculateAsync(TaxCalculationRequest req, CancellationToken ct)
    {
        // Live Stripe secret key, stored encrypted in the Credential Vault under "stripe-tax".
        var secretKey = await _vault.GetCredentialAsync(CredentialKey, ct);
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Stripe Tax credentials are not configured (Credential Vault key 'stripe-tax').");

        var client = _httpClientFactory.CreateClient("stripe-tax");
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);

        using var content = new FormUrlEncodedContent(BuildForm(req));
        using var response = await client.PostAsync(CalculationsUrl, content, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return MapResponse(document.RootElement);
    }

    // Stripe records a Tax Transaction from a prior calculation id, which is owned by the order/checkout
    // flow (Order Management). Remittance reporting is TaxJar-only, so this is intentionally a no-op.
    public Task ReportTransactionAsync(Guid orderId, CancellationToken ct) => Task.CompletedTask;

    private static List<KeyValuePair<string, string>> BuildForm(TaxCalculationRequest req)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("currency", "usd"),
            new("customer_details[address][country]", req.Destination.CountryCode),
            new("customer_details[address][state]", req.Destination.Region ?? string.Empty),
            new("customer_details[address][postal_code]", req.Destination.PostalCode ?? string.Empty),
            new("customer_details[address][city]", req.Destination.City ?? string.Empty),
            new("customer_details[address_source]", "shipping"),
            new("shipping_cost[amount]", ToMinorUnits(req.ShippingAmount)),
        };

        for (var i = 0; i < req.Lines.Count; i++)
        {
            var line = req.Lines[i];
            form.Add(new($"line_items[{i}][amount]", ToMinorUnits(line.Amount * line.Quantity)));
            form.Add(new($"line_items[{i}][reference]", line.ProductId.ToString()));
            if (!string.IsNullOrWhiteSpace(line.TaxCategoryCode))
                form.Add(new($"line_items[{i}][tax_code]", line.TaxCategoryCode));
        }

        return form;
    }

    private static string ToMinorUnits(decimal amount)
        => ((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Maps a Stripe Tax calculation response onto a <see cref="TaxBreakdown"/>. Shape (amounts in minor
    /// units): <c>{ "tax_amount_exclusive": 825, "tax_breakdown": [ { "amount": 825,
    /// "tax_rate_details": { "percentage_decimal": "8.25", "country": "US", "state": "CA" } } ] }</c>.
    /// </summary>
    private static TaxBreakdown MapResponse(JsonElement root)
    {
        var totalMinor = root.TryGetProperty("tax_amount_exclusive", out var t) && t.TryGetInt64(out var tv) ? tv : 0L;
        var totalTax = totalMinor / 100m;

        var jurisdictions = new List<TaxJurisdiction>();
        if (root.TryGetProperty("tax_breakdown", out var breakdown) && breakdown.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in breakdown.EnumerateArray())
            {
                var amountMinor = item.TryGetProperty("amount", out var a) && a.TryGetInt64(out var av) ? av : 0L;

                var rate = 0m;
                string? name = null;
                if (item.TryGetProperty("tax_rate_details", out var details) && details.ValueKind == JsonValueKind.Object)
                {
                    if (details.TryGetProperty("percentage_decimal", out var pct) && pct.ValueKind == JsonValueKind.String
                        && decimal.TryParse(pct.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pctVal))
                        rate = pctVal / 100m;

                    if (details.TryGetProperty("state", out var state) && state.ValueKind == JsonValueKind.String)
                        name = state.GetString();
                    else if (details.TryGetProperty("country", out var country) && country.ValueKind == JsonValueKind.String)
                        name = country.GetString();
                }

                jurisdictions.Add(new TaxJurisdiction(name ?? "Stripe Tax", "Tax", rate, amountMinor / 100m));
            }
        }

        return new TaxBreakdown(totalTax, jurisdictions, FallbackApplied: false);
    }
}
