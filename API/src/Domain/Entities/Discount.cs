using VSky.Domain.Common;
using VSky.Domain.Enums;

namespace VSky.Domain.Entities;

/// <summary>
/// A configurable discount rule applied at cart/checkout time (REQ-PRP-001). Non-exclusive discounts
/// stack; an active exclusive discount overrides the others (AC-PRP-001.4).
/// </summary>
public class Discount : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public DiscountScope Scope { get; set; }
    public DiscountType Type { get; set; }

    /// <summary>Percentage (0–100) when <see cref="Type"/> is Percentage; otherwise a fixed reduction.</summary>
    public decimal Value { get; set; }

    /// <summary>Target when <see cref="Scope"/> is Product.</summary>
    public Guid? ProductId { get; set; }
    /// <summary>Target when <see cref="Scope"/> is Category.</summary>
    public Guid? CategoryId { get; set; }

    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public decimal? MinimumOrderValue { get; set; }

    public bool IsExclusive { get; set; }
    public bool IsActive { get; set; } = true;

    public bool Deleted { get; set; }
    public DateTime? DeletedOnUtc { get; set; }

    public ICollection<CouponCode> CouponCodes { get; set; } = new List<CouponCode>();
}
