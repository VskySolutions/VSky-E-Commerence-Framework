using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// A physical/fulfilment store location used by the order routing engine
/// (Store Management blueprint).
/// </summary>
public class Store : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? OperatingHoursJson { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public string? CurrencyDisplay { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool MaintenanceMode { get; set; }
    public string? DeliveryZoneJson { get; set; }
    public int? OrderCapacityLimit { get; set; }

    /// <summary>
    /// Whether unauthenticated (guest) checkout is permitted for orders fulfilled by this store.
    /// Read by the Checkout Orchestrator after routing (WO-50 AC-STR-001.4/5).
    /// </summary>
    public bool GuestOrderingEnabled { get; set; } = true;

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<DeliveryZone> DeliveryZones { get; set; } = new List<DeliveryZone>();
}
