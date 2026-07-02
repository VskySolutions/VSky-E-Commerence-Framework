namespace VSky.Domain.Enums;

/// <summary>Built-in custom shipping method calculation type (REQ-SHP-003).</summary>
public enum ShippingMethodType
{
    FlatRate = 0,
    WeightBased = 1,
    PriceBased = 2,
    FreeShipping = 3
}
