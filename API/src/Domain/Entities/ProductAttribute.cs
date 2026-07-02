using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// A reusable product attribute in the global attribute library (e.g. "Colour", "Size") used to
/// generate product variants (REQ-CAT-003, AC-CAT-003.1).
/// </summary>
public class ProductAttribute : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
    public ICollection<ProductAttributeMapping> ProductMappings { get; set; } = new List<ProductAttributeMapping>();
}

/// <summary>A possible value of a <see cref="ProductAttribute"/> (e.g. "Red", "Large").</summary>
public class ProductAttributeValue : AuditableEntity
{
    public Guid ProductAttributeId { get; set; }
    public ProductAttribute? ProductAttribute { get; set; }
    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
