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

    public OrderStatus Status { get; set; } = OrderStatus.PendingRouting;

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
    public Store? AssignedStore { get; set; }
    public DateTime PlacedOnUtc { get; set; }
    public DateTime? RoutedOnUtc { get; set; }

    /// <summary>JSON array of store ids already attempted, so a rejection re-routes to the next store (AC-STR-003.4).</summary>
    public string? ExcludedStoreIdsJson { get; set; }

    public decimal TotalAmount { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<OrderLineItem> Lines { get; set; } = new List<OrderLineItem>();
}
