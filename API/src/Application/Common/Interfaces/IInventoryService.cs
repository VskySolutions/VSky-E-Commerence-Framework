namespace VSky.Application.Common.Interfaces;

/// <summary>
/// Per-store inventory tracking for products and variants (REQ-CAT-011, AC-CAT-011.1). Owns the
/// single write path for stock so counts stay consistent across the order lifecycle, and raises
/// low-stock alerts when a level falls to or below its threshold (AC-CAT-011.2).
/// <para>
/// <see cref="DecrementStockAsync"/>, <see cref="RestoreStockAsync"/> and
/// <see cref="MarkAsReceivedAsync"/> are the integration hooks the (future) order service calls at
/// order confirmation, pre-shipment cancellation and RMA acceptance respectively.
/// </para>
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Decrements the (product/variant, store) stock at order confirmation (AC-CAT-011.4). Returns
    /// <c>false</c> when available stock is insufficient and backorders are not allowed for the
    /// product/variant; otherwise decrements (possibly into a backorder) and returns <c>true</c>.
    /// </summary>
    Task<bool> DecrementStockAsync(Guid productId, Guid? variantId, Guid storeId, int quantity, CancellationToken ct = default);

    /// <summary>Restores stock when an order is cancelled before shipment (AC-CAT-011.5).</summary>
    Task RestoreStockAsync(Guid productId, Guid? variantId, Guid storeId, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Increments stock for accepted returned units on the RMA path; the only return flow that adds
    /// stock back (AC-CAT-011.6).
    /// </summary>
    Task MarkAsReceivedAsync(Guid productId, Guid? variantId, Guid storeId, int quantityAccepted, CancellationToken ct = default);

    /// <summary>
    /// Sets (creating when absent) the stock level and optional low-stock threshold for a
    /// (product/variant, store); the admin / store-manager path. Returns the new stock quantity.
    /// </summary>
    Task<int> SetStockLevelAsync(Guid productId, Guid? variantId, Guid storeId, int quantity, int? lowStockThreshold, CancellationToken ct = default);

    /// <summary>
    /// Applies a relative adjustment (positive or negative), creating the level when absent. Returns
    /// the new stock quantity.
    /// </summary>
    Task<int> AdjustStockAsync(Guid productId, Guid? variantId, Guid storeId, int delta, CancellationToken ct = default);

    /// <summary>
    /// Whether the requested quantity can be fulfilled from the (product/variant, store) — available
    /// stock meets demand or the product/variant allows backorders. Used by the order routing engine.
    /// </summary>
    Task<bool> IsAvailableAsync(Guid productId, Guid? variantId, Guid storeId, int quantity, CancellationToken ct = default);
}
