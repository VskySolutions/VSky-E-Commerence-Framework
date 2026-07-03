namespace VSky.Domain.Enums;

/// <summary>Built-in custom shipping method calculation type (REQ-SHP-003).</summary>
public enum ShippingMethodType
{
    FlatRate = 0,
    WeightBased = 1,
    PriceBased = 2,
    FreeShipping = 3
}

/// <summary>Lifecycle of a physical shipment / carrier package (REQ-ORD-002, REQ-SHP-002).</summary>
public enum ShipmentStatus
{
    Created = 0,
    LabelGenerated = 1,
    Shipped = 2,
    InTransit = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Exception = 6,
    Cancelled = 7
}
