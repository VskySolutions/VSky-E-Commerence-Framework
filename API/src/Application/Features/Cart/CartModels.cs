using CartEntity = VSky.Domain.Entities.Cart;
using CartItemEntity = VSky.Domain.Entities.CartItem;

namespace VSky.Application.Features.Cart;

/// <summary>
/// A shopping cart projection returned by every Cart endpoint (REQ-CHK-001). Carries the resolved
/// lines, the computed <see cref="Subtotal"/> and any availability <see cref="Warnings"/> raised while
/// re-checking each line against the live catalog (AC-CHK-001.5/6).
/// </summary>
public class CartDto
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string? SessionId { get; set; }
    public string? AppliedCouponCode { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public List<CartItemDto> Items { get; set; } = new();

    /// <summary>Sum of every line's <see cref="CartItemDto.LineTotal"/>.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Human-readable notices for unavailable, unpublished or out-of-stock lines (AC-CHK-001.5).</summary>
    public List<string> Warnings { get; set; } = new();

    public static CartDto From(CartEntity cart, List<CartItemDto> items, List<string> warnings) => new()
    {
        Id = cart.Id,
        CustomerId = cart.CustomerId,
        SessionId = cart.SessionId,
        AppliedCouponCode = cart.AppliedCouponCode,
        CurrencyCode = cart.CurrencyCode,
        Items = items,
        Subtotal = items.Sum(i => i.LineTotal),
        Warnings = warnings,
    };
}

/// <summary>A single line in a <see cref="CartDto"/> with its snapshotted unit price (AC-CHK-001.1).</summary>
public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>False when the line's product/variant is missing, unpublished or out of stock (AC-CHK-001.5).</summary>
    public bool Available { get; set; }

    public static CartItemDto From(CartItemEntity item, string productName, string? sku, bool available) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductVariantId = item.ProductVariantId,
        ProductName = productName,
        Sku = sku,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice,
        LineTotal = item.UnitPrice * item.Quantity,
        Available = available,
    };
}
