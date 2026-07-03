namespace VSky.Domain.Enums;

/// <summary>How a customer role adjusts prices for its members (REQ-CUS-003).</summary>
public enum CustomerRolePricingRuleType
{
    /// <summary>No price adjustment (the role is for access control only).</summary>
    None = 0,

    /// <summary>A flat percentage off the base price (DiscountPercent).</summary>
    PercentageDiscount = 1,

    /// <summary>Explicit per-product/variant group prices (CustomerGroupPrice rows).</summary>
    FixedGroupPrice = 2
}
