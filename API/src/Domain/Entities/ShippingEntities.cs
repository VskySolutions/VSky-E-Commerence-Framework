using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// A built-in (non-carrier) shipping method: flat rate, weight-based, price-based, or free-shipping
/// threshold (REQ-SHP-003). Weight/price tiers are stored as JSON in <see cref="TiersJson"/>.
/// </summary>
public class ShippingMethod : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public ShippingMethodType MethodType { get; set; }

    /// <summary>Cost for <see cref="ShippingMethodType.FlatRate"/>.</summary>
    public decimal? FlatRate { get; set; }
    /// <summary>Order-subtotal threshold that unlocks free shipping.</summary>
    public decimal? FreeShippingThreshold { get; set; }
    /// <summary>JSON tier array for weight/price-based methods, e.g. [{"min":0,"max":5,"rate":9.99}].</summary>
    public string? TiersJson { get; set; }

    public bool IsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<ShippingMethodZoneRate> ZoneRates { get; set; } = new List<ShippingMethodZoneRate>();
}

/// <summary>A geographic shipping zone by country + optional region/postal-code range (AC-SHP-003.5).</summary>
public class ShippingZone : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? PostalCodeStart { get; set; }
    public string? PostalCodeEnd { get; set; }
    public bool IsEnabled { get; set; } = true;

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<ShippingMethodZoneRate> MethodRates { get; set; } = new List<ShippingMethodZoneRate>();
}

/// <summary>A per-zone rate override for a shipping method (AC-SHP-003.5).</summary>
public class ShippingMethodZoneRate : BaseEntity
{
    public Guid ShippingMethodId { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }
    public Guid ShippingZoneId { get; set; }
    public ShippingZone? ShippingZone { get; set; }
    public decimal Rate { get; set; }
}
