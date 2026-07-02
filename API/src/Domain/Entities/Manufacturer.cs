using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// A brand/maker record associable to products and filterable on the storefront (REQ-CAT-005).
/// </summary>
public class Manufacturer : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }

    // SEO metadata (AC-CAT-005.1)
    public string? Slug { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
