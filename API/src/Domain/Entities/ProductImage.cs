using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// An image or video-embed entry in a product/variant gallery with a display order (REQ-CAT-012).
/// When <see cref="ProductVariantId"/> is set the media is shown for that variant selection.
/// </summary>
public class ProductImage : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Optional variant this media belongs to (AC-CAT-002.5); null = product-level.</summary>
    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public ProductMediaType MediaType { get; set; } = ProductMediaType.Image;

    /// <summary>Image URL, or the video embed URL for a <see cref="ProductMediaType.Video"/> entry (AC-CAT-012.2).</summary>
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
}
