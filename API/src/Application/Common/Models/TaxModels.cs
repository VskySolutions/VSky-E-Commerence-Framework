namespace VSky.Application.Common.Models;

/// <summary>
/// An origin (ship-from) or destination (ship-to) address used to resolve the applicable tax
/// jurisdictions for a calculation (REQ-TAX-001).
/// </summary>
public record TaxAddress(
    string CountryCode,
    string? Region = null,
    string? PostalCode = null,
    string? City = null);

/// <summary>
/// A single taxable line in a <see cref="TaxCalculationRequest"/>. <see cref="Amount"/> is the
/// per-unit price; the extended taxable amount is <see cref="Amount"/> × <see cref="Quantity"/>.
/// <see cref="TaxCategoryCode"/> is the provider tax code the product's Tax Category maps to.
/// </summary>
public record TaxLineInput(
    Guid ProductId,
    string? TaxCategoryCode,
    decimal Amount,
    int Quantity);

/// <summary>
/// Input to a tax calculation: the ship-from <see cref="Origin"/>, ship-to <see cref="Destination"/>,
/// the taxable <see cref="Lines"/> and the (separately taxable) <see cref="ShippingAmount"/>.
/// </summary>
public record TaxCalculationRequest(
    TaxAddress Origin,
    TaxAddress Destination,
    List<TaxLineInput> Lines,
    decimal ShippingAmount);

/// <summary>
/// One jurisdiction's contribution to the total tax (e.g. country/state/county/city/special district).
/// <see cref="Rate"/> is a decimal fraction (0.0825 = 8.25%).
/// </summary>
public record TaxJurisdiction(
    string Name,
    string? Type,
    decimal Rate,
    decimal Amount);

/// <summary>
/// Result of a tax calculation: the <see cref="TotalTax"/> to collect, the per-jurisdiction
/// <see cref="Jurisdictions"/> breakdown, and <see cref="FallbackApplied"/> — set when the flat-rate
/// fallback was used because the active provider was unavailable (AC-TAX-001.4).
/// </summary>
public record TaxBreakdown(
    decimal TotalTax,
    List<TaxJurisdiction> Jurisdictions,
    bool FallbackApplied);
