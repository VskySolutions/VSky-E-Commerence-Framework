using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// A line within an <see cref="Order"/>. Product name/SKU/price are snapshotted at placement time so
/// the order is immutable to later catalog edits. <see cref="ProductId"/>/<see cref="ProductVariantId"/>
/// are stored as plain references (no FK) to preserve the order if a product is later removed.
/// </summary>
public class OrderLineItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Units of this line already dispatched across shipments; drives partial-fulfilment state (WO-46).</summary>
    public int QuantityShipped { get; set; }
}
