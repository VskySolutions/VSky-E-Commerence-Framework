using VSky.Domain.Entities;

namespace VSky.Application.Features.ProductAttributes;

/// <summary>A reusable product attribute (e.g. "Colour", "Size") and its selectable values.</summary>
public class ProductAttributeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public List<ProductAttributeValueDto> Values { get; set; } = new();

    public static ProductAttributeDto From(ProductAttribute a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Description = a.Description,
        DisplayOrder = a.DisplayOrder,
        Values = a.Values
            .OrderBy(v => v.DisplayOrder)
            .Select(ProductAttributeValueDto.From)
            .ToList(),
    };
}

/// <summary>A single selectable value of a <see cref="ProductAttributeDto"/> (e.g. "Red", "Large").</summary>
public class ProductAttributeValueDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public static ProductAttributeValueDto From(ProductAttributeValue v) => new()
    {
        Id = v.Id,
        Value = v.Value,
        DisplayOrder = v.DisplayOrder,
    };
}
