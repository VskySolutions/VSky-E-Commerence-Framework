namespace VSky.Domain.Enums;

/// <summary>
/// How a product attribute's values are presented for variant selection on the storefront and in the
/// admin attribute library (WO-15, REQ-CAT-003). Swatch values additionally carry a colour (ColorHex).
/// </summary>
public enum ProductAttributeDisplayType
{
    Dropdown = 0,
    Button = 1,
    Swatch = 2,
}
