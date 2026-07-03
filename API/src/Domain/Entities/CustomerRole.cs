using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// An admin-defined customer group carrying group-pricing rules and catalog-access restrictions
/// (REQ-CUS-003). Distinct from the admin RBAC <see cref="Role"/>. A customer may belong to several roles.
/// </summary>
public class CustomerRole : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public CustomerRolePricingRuleType PricingRuleType { get; set; } = CustomerRolePricingRuleType.None;

    /// <summary>Percentage off the base price when <see cref="PricingRuleType"/> is PercentageDiscount (0–100).</summary>
    public decimal? DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<CustomerGroupPrice> GroupPrices { get; set; } = new List<CustomerGroupPrice>();
}

/// <summary>Assigns a <see cref="Customer"/> to a <see cref="CustomerRole"/> (many-to-many) (AC-CUS-003.2).</summary>
public class CustomerRoleAssignment : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid CustomerRoleId { get; set; }
    public CustomerRole? CustomerRole { get; set; }
}

/// <summary>An explicit price for a product/variant for members of a role (AC-CUS-003.1/003.3).</summary>
public class CustomerGroupPrice : BaseEntity
{
    public Guid CustomerRoleId { get; set; }
    public CustomerRole? CustomerRole { get; set; }

    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public decimal Price { get; set; }
}

/// <summary>Restricts a product to members of a role (AC-CUS-003.4); no rows = visible to everyone.</summary>
public class ProductRoleRestriction : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid CustomerRoleId { get; set; }
    public CustomerRole? CustomerRole { get; set; }
}

/// <summary>Restricts a category to members of a role (AC-CUS-003.4); no rows = visible to everyone.</summary>
public class CategoryRoleRestriction : BaseEntity
{
    public Guid CategoryId { get; set; }
    public Guid CustomerRoleId { get; set; }
    public CustomerRole? CustomerRole { get; set; }
}
