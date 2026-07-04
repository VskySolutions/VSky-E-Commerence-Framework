using VSky.Application.Common.Models;
using VSky.Application.Tests.Common;
using VSky.Domain.Entities;
using VSky.Domain.Enums;
using VSky.Infrastructure.Pricing;
using Xunit;

namespace VSky.Application.Tests.Pricing;

/// <summary>
/// Coupon-gating behavior of the discount engine (WO-25 / REQ-PRP-002). A discount flagged
/// <see cref="Discount.RequiresCoupon"/> must never apply automatically — it only takes effect when
/// its id is passed as an "unlocked" (valid-coupon-bound) discount. Auto-apply discounts are
/// unaffected. Backed by the real SQL Server test database via <see cref="CatalogTestBase"/>.
/// </summary>
public class DiscountServiceTests : CatalogTestBase
{
    private Guid SeedDiscount(bool requiresCoupon, decimal value = 10m, string name = "Discount")
    {
        using var db = NewContext();
        var d = new Discount
        {
            Name = name,
            Scope = DiscountScope.CartTotal,
            Type = DiscountType.Percentage,
            Value = value,
            IsActive = true,
            RequiresCoupon = requiresCoupon,
        };
        db.Discounts.Add(d);
        db.SaveChanges();
        return d.Id;
    }

    private static IReadOnlyList<DiscountCartLine> Lines(decimal total) =>
        new List<DiscountCartLine> { new(Guid.NewGuid(), new List<Guid>(), total, 1) };

    [Fact]
    public async Task CouponGatedDiscount_IsSkipped_WhenNoCouponUnlocksIt()
    {
        SeedDiscount(requiresCoupon: true, value: 10m);
        var svc = new DiscountService(NewContext());

        var result = await svc.EvaluateAsync(Lines(100m), 100m);

        Assert.Empty(result.Applied);
        Assert.Equal(0m, result.TotalDiscount);
    }

    [Fact]
    public async Task CouponGatedDiscount_IsApplied_WhenItsIdIsUnlocked()
    {
        var id = SeedDiscount(requiresCoupon: true, value: 10m);
        var svc = new DiscountService(NewContext());

        var result = await svc.EvaluateAsync(Lines(100m), 100m, new[] { id });

        var applied = Assert.Single(result.Applied);
        Assert.Equal(id, applied.DiscountId);
        Assert.Equal(10m, result.TotalDiscount); // 10% of 100
    }

    [Fact]
    public async Task AutoApplyDiscount_Applies_WithoutAnyCoupon()
    {
        SeedDiscount(requiresCoupon: false, value: 15m);
        var svc = new DiscountService(NewContext());

        var result = await svc.EvaluateAsync(Lines(100m), 100m);

        Assert.Single(result.Applied);
        Assert.Equal(15m, result.TotalDiscount);
    }

    [Fact]
    public async Task UnlockingOneGatedDiscount_DoesNotUnlockAnother()
    {
        var unlocked = SeedDiscount(requiresCoupon: true, value: 10m, name: "Unlocked");
        SeedDiscount(requiresCoupon: true, value: 25m, name: "Still locked");
        var svc = new DiscountService(NewContext());

        var result = await svc.EvaluateAsync(Lines(100m), 100m, new[] { unlocked });

        var applied = Assert.Single(result.Applied);
        Assert.Equal(unlocked, applied.DiscountId);
    }
}
