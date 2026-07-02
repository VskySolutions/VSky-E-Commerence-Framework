using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// A catalog product of any of the five <see cref="ProductType"/> kinds (REQ-CAT-001). Simple
/// products carry SKU/price/stock directly; variant products delegate those to <see cref="ProductVariant"/>;
/// grouped products aggregate member products; downloadable and gift-card products carry type-specific config.
/// Every product must reference a <see cref="TaxCategory"/> (AC-CAT-001.6).
/// </summary>
public class Product : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public ProductType ProductType { get; set; } = ProductType.Simple;

    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }

    // Simple-product fields (also the default/base for other types).
    public string? Sku { get; set; }
    public decimal? Price { get; set; }
    public int StockQuantity { get; set; }

    /// <summary>Whether this product may be ordered when its inventory reaches zero (AC-CAT-011.3).</summary>
    public bool AllowBackorder { get; set; }
    public DateTime? EstimatedRestockDate { get; set; }

    // Tax (required) and manufacturer (optional).
    public Guid TaxCategoryId { get; set; }
    public TaxCategory? TaxCategory { get; set; }
    public Guid? ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    public bool IsPublished { get; set; }
    public bool ReviewsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; }

    // Downloadable-product config (AC-CAT-001.4).
    public int? DownloadExpiryDays { get; set; }
    public int? DownloadLimit { get; set; }

    // Gift-card config (AC-CAT-001.5).
    public GiftCardType? GiftCardType { get; set; }
    public decimal? GiftCardAmount { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    // Navigation collections.
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<TierPrice> TierPrices { get; set; } = new List<TierPrice>();
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    public ICollection<ProductTagMapping> Tags { get; set; } = new List<ProductTagMapping>();
    public ICollection<ProductAttributeMapping> AttributeMappings { get; set; } = new List<ProductAttributeMapping>();
    public ICollection<ProductSpecificationValue> SpecificationValues { get; set; } = new List<ProductSpecificationValue>();
    public ICollection<ProductRelationship> Relationships { get; set; } = new List<ProductRelationship>();
    public ICollection<GroupedProductMember> GroupedMembers { get; set; } = new List<GroupedProductMember>();
    public ICollection<ProductDownload> Downloads { get; set; } = new List<ProductDownload>();
    public ICollection<InventoryLevel> InventoryLevels { get; set; } = new List<InventoryLevel>();
}
