using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// A minimal customer order used by the routing engine (WO-51) and store order queue (WO-52). Carries
/// the delivery address (for routing), the assigned store, line items, and a lifecycle
/// <see cref="OrderStatus"/>. Full checkout/payment/pricing is a separate future feature — this model
/// is intentionally lean and additive.
/// </summary>
public class Order : AuditableEntity, ISoftDeletable
{
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Null for a guest order.</summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // Delivery target (drives routing).
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? CountryCode { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }

    // Routing outcome.
    public Guid? AssignedStoreId { get; set; }

    /// <summary>True when the order is fulfilled by pickup-in-store rather than carrier delivery (REQ-SHP-004).</summary>
    public bool IsPickup { get; set; }
    public Store? AssignedStore { get; set; }
    public DateTime PlacedOnUtc { get; set; }
    public DateTime? RoutedOnUtc { get; set; }

    /// <summary>JSON array of store ids already attempted, so a rejection re-routes to the next store (AC-STR-003.4).</summary>
    public string? ExcludedStoreIdsJson { get; set; }

    // Financial breakdown, captured at placement (WO-30/45). TotalAmount is the grand total.
    public string CurrencyCode { get; set; } = "USD";
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Immutable jurisdiction-level tax breakdown JSON, stored at placement time and never recalculated (Order Management ADR).</summary>
    public string? TaxBreakdownJson { get; set; }
    /// <summary>Set when the flat-rate tax fallback was applied and the order needs tax review (AC-TAX-001.4).</summary>
    public bool TaxFlaggedForReview { get; set; }
    /// <summary>Provider calculation reference captured at placement, used for transaction reporting (e.g. Stripe Tax calculation id; WO-37).</summary>
    public string? TaxProviderCalculationRef { get; set; }

    public string? AppliedCouponCode { get; set; }

    // Shipping selection + fulfilment tracking (WO-45).
    public string? ShippingMethodName { get; set; }
    public string? ShippingCarrier { get; set; }
    public string? TrackingNumber { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime? ShippedOnUtc { get; set; }
    public DateTime? DeliveredOnUtc { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<OrderLineItem> Lines { get; set; } = new List<OrderLineItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<PaymentRecord> Payments { get; set; } = new List<PaymentRecord>();
}
