namespace VSky.Application.Common.Models;

/// <summary>
/// An origin or destination point for a shipping-rate quote: country/region/postal for zone matching
/// and carrier rating, plus optional geo-coordinates (WO-40).
/// </summary>
public record CarrierAddress(
    string? CountryCode,
    string? Region,
    string? PostalCode,
    double? Latitude,
    double? Longitude);

/// <summary>
/// Input to the shipping-rate service: the shipment's origin and destination, the package weight and
/// optional dimensions, and the order subtotal used to evaluate free-shipping and price-based custom
/// methods (WO-40).
/// </summary>
public record CarrierRateRequest(
    CarrierAddress Origin,
    CarrierAddress Destination,
    decimal WeightKg,
    decimal? Length,
    decimal? Width,
    decimal? Height,
    decimal OrderSubtotal);

/// <summary>
/// A single quotable shipping option — either an enabled custom method or a live-carrier product —
/// returned by <see cref="Interfaces.IShippingRateService"/> and the carrier adapters (WO-40).
/// </summary>
public record ShippingRateOption(
    string MethodId,
    string Name,
    string Carrier,
    int? EstimatedDeliveryDays,
    decimal Rate);
