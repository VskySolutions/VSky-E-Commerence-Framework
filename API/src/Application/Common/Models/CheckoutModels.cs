using VSky.Domain.Enums;

namespace VSky.Application.Common.Models;

/// <summary>
/// A ship-to (and contact) address supplied at checkout (REQ-CHK-003). Carries the buyer's name/email
/// for the order contact, the postal address used for routing, shipping and tax, and optional
/// geo-coordinates that improve nearest-store routing and carrier rating.
/// </summary>
public record CheckoutAddress(
    string FirstName,
    string LastName,
    string Email,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode,
    double? Latitude = null,
    double? Longitude = null,
    string? Landmark = null,
    string? PhoneNumber = null);

/// <summary>
/// Input to a checkout price quote (REQ-CHK-003): the cart to price (by <see cref="CartId"/> or guest
/// <see cref="SessionId"/>, or the current customer's active cart when both are null), the delivery
/// <see cref="ShipTo"/> address, and an optional preferred <see cref="ShippingMethodId"/>.
/// </summary>
public record CheckoutQuoteRequest(
    Guid? CartId,
    string? SessionId,
    CheckoutAddress ShipTo,
    string? ShippingMethodId,
    bool PickupInStore = false,
    Guid? PickupStoreId = null);

/// <summary>
/// The priced outcome of a checkout quote: the cart subtotal, itemized discounts, the available
/// shipping options and the selected shipping cost, the tax breakdown, and the grand total — plus the
/// routing outcome (<see cref="AssignedStoreId"/>/<see cref="IsRoutable"/>) and whether guest ordering
/// is permitted for the assigned store (<see cref="GuestOrderingAllowed"/>, AC-CHK-003.2).
/// </summary>
public class CheckoutQuote
{
    public decimal Subtotal { get; set; }
    public List<AppliedDiscount> Discounts { get; set; } = new();
    public decimal DiscountTotal { get; set; }
    public List<ShippingRateOption> ShippingOptions { get; set; } = new();
    public decimal ShippingTotal { get; set; }
    public TaxBreakdown Tax { get; set; } = new(0m, new(), false);
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public Guid? AssignedStoreId { get; set; }
    public bool IsRoutable { get; set; }
    public bool GuestOrderingAllowed { get; set; }
}

/// <summary>
/// Request to place (finalize) a checkout (REQ-CHK-003): the cart identity, the delivery
/// <see cref="ShipTo"/> address, the chosen <see cref="SelectedShippingMethodId"/>, the
/// <see cref="PaymentMethod"/> and optional gateway <see cref="PaymentToken"/>, and an optional
/// <see cref="CouponCode"/> (overrides any coupon already applied to the cart).
/// </summary>
public record PlaceCheckoutRequest(
    Guid? CartId,
    string? SessionId,
    CheckoutAddress ShipTo,
    string? SelectedShippingMethodId,
    PaymentMethodType PaymentMethod,
    string? PaymentToken,
    string? CouponCode,
    string? RecaptchaToken = null,
    bool PickupInStore = false,
    Guid? PickupStoreId = null);

/// <summary>
/// The outcome of placing a checkout: the created order's id/number/status and grand total, the
/// resulting payment status, and a <see cref="Success"/> flag with an optional <see cref="Error"/> —
/// a failed payment returns <see cref="Success"/> = false while leaving a retryable pending order
/// (AC-PAY-001.3).
/// </summary>
public class CheckoutResult
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Error { get; set; }
}
