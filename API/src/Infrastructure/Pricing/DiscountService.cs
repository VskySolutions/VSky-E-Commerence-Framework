using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Pricing;

/// <summary>
/// Applies the configured discount rules to a cart (REQ-PRP-001). Loads every active, in-window
/// discount whose minimum-order threshold the subtotal meets, computes each rule's reduction against
/// the base its scope targets, then either stacks all non-exclusive reductions or — when any
/// exclusive discount applies — replaces them with the single most-favorable exclusive one
/// (AC-PRP-001.4). The result is an itemized breakdown whose total equals the sum of the amounts.
/// </summary>
public class DiscountService : IDiscountService
{
    private readonly IApplicationDbContext _db;

    public DiscountService(IApplicationDbContext db) => _db = db;

    public async Task<DiscountEvaluationResult> EvaluateAsync(
        IReadOnlyList<DiscountCartLine> lines, decimal subtotal,
        IReadOnlyCollection<Guid>? unlockedDiscountIds = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Candidate rules: active, currently in-window, and satisfied by the subtotal's minimum threshold.
        var discounts = await _db.Discounts
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Where(d => d.StartDateUtc == null || d.StartDateUtc <= now)
            .Where(d => d.EndDateUtc == null || d.EndDateUtc >= now)
            .Where(d => d.MinimumOrderValue == null || subtotal >= d.MinimumOrderValue)
            .ToListAsync(ct);

        // Turn each rule into a concrete reduction; drop rules that reduce nothing (no matching lines, etc.).
        var candidates = new List<(Discount Discount, AppliedDiscount Applied)>();
        foreach (var d in discounts)
        {
            // Coupon-gated rules (REQ-PRP-002) apply only when a valid coupon bound to them is present;
            // otherwise they never contribute, so entering the code is what actually unlocks the price.
            if (d.RequiresCoupon && (unlockedDiscountIds is null || !unlockedDiscountIds.Contains(d.Id)))
                continue;

            var amount = ComputeReduction(d, lines, subtotal);
            if (amount <= 0m)
                continue;
            candidates.Add((d, new AppliedDiscount(d.Id, d.Name, d.Scope, amount)));
        }

        // AC-PRP-001.4: an applicable exclusive discount overrides all others — keep only the best one.
        var exclusive = candidates.Where(c => c.Discount.IsExclusive).ToList();
        if (exclusive.Count > 0)
        {
            var best = exclusive
                .OrderByDescending(c => c.Applied.Amount)
                .ThenBy(c => c.Applied.DiscountId)
                .First()
                .Applied;
            return new DiscountEvaluationResult(new List<AppliedDiscount> { best }, best.Amount);
        }

        // Otherwise every non-exclusive discount stacks.
        var applied = candidates.Select(c => c.Applied).ToList();
        var total = applied.Sum(a => a.Amount);
        return new DiscountEvaluationResult(applied, total);
    }

    /// <summary>
    /// The reduction a single rule contributes: a percentage of, or a fixed amount capped at, the base
    /// its scope targets (whole subtotal for cart/order rules, matching product lines for product rules,
    /// matching-category lines for category rules). Never reduces the targeted base below zero.
    /// </summary>
    private static decimal ComputeReduction(Discount d, IReadOnlyList<DiscountCartLine> lines, decimal subtotal)
    {
        var @base = d.Scope switch
        {
            DiscountScope.CartTotal or DiscountScope.OrderSubtotal => subtotal,
            DiscountScope.Product => lines
                .Where(l => d.ProductId.HasValue && l.ProductId == d.ProductId.Value)
                .Sum(l => l.LineTotal),
            DiscountScope.Category => lines
                .Where(l => d.CategoryId.HasValue && l.CategoryIds.Contains(d.CategoryId.Value))
                .Sum(l => l.LineTotal),
            _ => 0m,
        };

        if (@base <= 0m)
            return 0m;

        var reduction = d.Type switch
        {
            DiscountType.Percentage => @base * (d.Value / 100m),
            DiscountType.FixedAmount => d.Value,
            _ => 0m,
        };

        if (reduction < 0m)
            reduction = 0m;
        if (reduction > @base)
            reduction = @base;

        return decimal.Round(reduction, 2, MidpointRounding.AwayFromZero);
    }
}
