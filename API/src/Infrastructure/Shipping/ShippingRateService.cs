using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Shipping;

/// <summary>
/// Aggregates shipping options for a shipment (WO-40): first the enabled custom shipping methods
/// (flat / weight / price / free, with per-zone rates and zone eligibility), then every configured live
/// carrier queried in parallel. Carriers that throw or return nothing are silently excluded so a single
/// failing integration never blocks checkout (AC-SHP-001.3).
/// </summary>
public class ShippingRateService : IShippingRateService
{
    private static readonly JsonSerializerOptions TierJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IApplicationDbContext _db;
    private readonly IEnumerable<ICarrierClient> _carriers;

    public ShippingRateService(IApplicationDbContext db, IEnumerable<ICarrierClient> carriers)
    {
        _db = db;
        _carriers = carriers;
    }

    public async Task<IReadOnlyList<ShippingRateOption>> GetRatesAsync(CarrierRateRequest request, CancellationToken ct)
    {
        var options = new List<ShippingRateOption>();
        options.AddRange(await EvaluateCustomMethodsAsync(request, ct));

        // Query every configured carrier concurrently; a carrier that throws or returns nothing is
        // silently excluded from the aggregate (AC-SHP-001.3).
        var carrierResults = await Task.WhenAll(_carriers.Select(c => SafeGetRatesAsync(c, request, ct)));
        foreach (var result in carrierResults)
            options.AddRange(result);

        return options;
    }

    private async Task<IReadOnlyList<ShippingRateOption>> EvaluateCustomMethodsAsync(CarrierRateRequest request, CancellationToken ct)
    {
        var methods = await _db.ShippingMethods
            .AsNoTracking()
            .Include(m => m.ZoneRates)
                .ThenInclude(r => r.ShippingZone)
            .Where(m => m.IsEnabled)
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Name)
            .ToListAsync(ct);

        var destination = request.Destination;
        var results = new List<ShippingRateOption>();

        foreach (var method in methods)
        {
            var matchedZoneRate = method.ZoneRates.FirstOrDefault(zr => ZoneMatches(zr.ShippingZone, destination));

            // Zone eligibility (AC-SHP-003.6): a method that carries any per-zone rate is only offered
            // when the destination falls inside one of those zones (hide out-of-zone).
            if (method.ZoneRates.Count > 0 && matchedZoneRate is null)
                continue;

            var rate = method.MethodType switch
            {
                ShippingMethodType.FlatRate => matchedZoneRate?.Rate ?? method.FlatRate,
                ShippingMethodType.FreeShipping =>
                    request.OrderSubtotal >= (method.FreeShippingThreshold ?? 0m) ? 0m : (decimal?)null,
                ShippingMethodType.WeightBased => PickTierRate(method.TiersJson, request.WeightKg),
                ShippingMethodType.PriceBased => PickTierRate(method.TiersJson, request.OrderSubtotal),
                _ => null,
            };

            if (rate is null)
                continue;

            results.Add(new ShippingRateOption(
                MethodId: method.Id.ToString(),
                Name: method.Name,
                Carrier: "Custom",
                EstimatedDeliveryDays: null,
                Rate: rate.Value));
        }

        return results;
    }

    private static async Task<IReadOnlyList<ShippingRateOption>> SafeGetRatesAsync(
        ICarrierClient client, CarrierRateRequest request, CancellationToken ct)
    {
        try
        {
            return await client.GetRatesAsync(request, ct);
        }
        catch
        {
            // Exclude a failing carrier from the aggregate rather than fail the whole quote.
            return Array.Empty<ShippingRateOption>();
        }
    }

    /// <summary>True when a zone is enabled and its country / region / postal-range contains the destination.</summary>
    private static bool ZoneMatches(ShippingZone? zone, CarrierAddress destination)
    {
        if (zone is null || !zone.IsEnabled)
            return false;
        if (string.IsNullOrWhiteSpace(destination.CountryCode))
            return false;
        if (!string.Equals(zone.CountryCode, destination.CountryCode, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrWhiteSpace(zone.Region) &&
            !string.Equals(zone.Region, destination.Region, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrWhiteSpace(zone.PostalCodeStart) &&
            !string.IsNullOrWhiteSpace(zone.PostalCodeEnd) &&
            !string.IsNullOrWhiteSpace(destination.PostalCode))
        {
            if (string.Compare(destination.PostalCode, zone.PostalCodeStart, StringComparison.OrdinalIgnoreCase) < 0 ||
                string.Compare(destination.PostalCode, zone.PostalCodeEnd, StringComparison.OrdinalIgnoreCase) > 0)
                return false;
        }
        return true;
    }

    /// <summary>Selects the first tier whose [Min,Max] range contains the value (weight or subtotal).</summary>
    private static decimal? PickTierRate(string? tiersJson, decimal value)
    {
        if (string.IsNullOrWhiteSpace(tiersJson))
            return null;

        List<ShippingTier>? tiers;
        try
        {
            tiers = JsonSerializer.Deserialize<List<ShippingTier>>(tiersJson, TierJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (tiers is null)
            return null;

        foreach (var tier in tiers.OrderBy(t => t.Min))
        {
            if (value >= tier.Min && value <= tier.Max)
                return tier.Rate;
        }

        return null;
    }

    /// <summary>A single weight/price tier row parsed from <see cref="ShippingMethod.TiersJson"/>.</summary>
    private sealed record ShippingTier(decimal Min, decimal Max, decimal Rate);
}
