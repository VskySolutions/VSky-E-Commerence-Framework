using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.CustomerRoles;

/// <summary>A customer role with its pricing rule and group prices (REQ-CUS-003).</summary>
public class CustomerRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CustomerRolePricingRuleType PricingRuleType { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public List<CustomerGroupPriceDto> GroupPrices { get; set; } = new();

    public static CustomerRoleDto From(CustomerRole r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        PricingRuleType = r.PricingRuleType,
        DiscountPercent = r.DiscountPercent,
        IsActive = r.IsActive,
        DisplayOrder = r.DisplayOrder,
        GroupPrices = r.GroupPrices.Select(CustomerGroupPriceDto.From).ToList(),
    };
}

public class CustomerGroupPriceDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public decimal Price { get; set; }

    public static CustomerGroupPriceDto From(CustomerGroupPrice g) => new()
    {
        Id = g.Id,
        ProductId = g.ProductId,
        ProductVariantId = g.ProductVariantId,
        Price = g.Price,
    };
}

/// <summary>An input group-price row for the set-group-prices command.</summary>
public record CustomerGroupPriceInput(Guid ProductId, Guid? ProductVariantId, decimal Price);
