using VSky.Application.Common.Models;

namespace VSky.Application.Common.Interfaces;

/// <summary>
/// A live-carrier rating adapter (e.g. DHL Express, UPS). Implementations call the carrier's REST
/// rating API and map the response to <see cref="ShippingRateOption"/>s (WO-40). A carrier that is
/// unconfigured or that fails is expected to return an empty list rather than throw, so the aggregating
/// service can simply exclude it (AC-SHP-001.3).
/// </summary>
public interface ICarrierClient
{
    /// <summary>Human-readable carrier name (e.g. "DHL Express", "UPS").</summary>
    string CarrierName { get; }

    /// <summary>Requests live rate options for a shipment; returns an empty list when unavailable.</summary>
    Task<IReadOnlyList<ShippingRateOption>> GetRatesAsync(CarrierRateRequest request, CancellationToken ct);
}
