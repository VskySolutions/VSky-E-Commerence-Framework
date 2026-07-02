using VSky.Domain.Common;

namespace VSky.Domain.Entities;

/// <summary>
/// A shopping cart owned by an authenticated customer or an anonymous session (REQ-CHK-001).
/// Persisted so a registered buyer's cart is restored on return (AC-CHK-001.4).
/// </summary>
public class Cart : AuditableEntity, ISoftDeletable
{
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Anonymous session identifier for guest carts (null once associated with a customer).</summary>
    public string? SessionId { get; set; }

    public string? AppliedCouponCode { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsCheckedOut { get; set; }

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

/// <summary>A line in a <see cref="Cart"/> with the unit price snapshotted when added (AC-CHK-001.1).</summary>
public class CartItem : AuditableEntity
{
    public Guid CartId { get; set; }
    public Cart? Cart { get; set; }

    public Guid ProductId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
