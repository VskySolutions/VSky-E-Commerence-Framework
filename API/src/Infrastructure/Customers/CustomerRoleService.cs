using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Customers;

/// <summary>
/// Role-based pricing + access resolution (REQ-CUS-003). Group prices and percentage-discount roles both
/// feed the "best price" calculation; restriction rows gate product/category visibility. All lookups exclude
/// inactive/soft-deleted roles (the CustomerRole query filter handles the latter).
/// </summary>
public class CustomerRoleService : ICustomerRoleService
{
    private readonly IApplicationDbContext _db;

    public CustomerRoleService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Guid>> GetCustomerRoleIdsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _db.CustomerRoleAssignments
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Join(_db.CustomerRoles.Where(r => r.IsActive), a => a.CustomerRoleId, r => r.Id, (a, r) => r.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> ResolvePriceAsync(
        Guid productId, Guid? variantId, decimal basePrice, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        if (roleIds.Count == 0)
            return basePrice;

        var best = basePrice;

        // Explicit group prices for these roles (variant-specific wins, else product-level).
        var groupPrices = await _db.CustomerGroupPrices
            .AsNoTracking()
            .Where(g => roleIds.Contains(g.CustomerRoleId) && g.ProductId == productId
                        && (g.ProductVariantId == variantId || g.ProductVariantId == null))
            .Select(g => new { g.ProductVariantId, g.Price })
            .ToListAsync(cancellationToken);

        // Prefer a variant-specific group price when present.
        var variantPrices = groupPrices.Where(g => g.ProductVariantId == variantId).Select(g => g.Price).ToList();
        var applicable = variantPrices.Count > 0 ? variantPrices : groupPrices.Select(g => g.Price).ToList();
        if (applicable.Count > 0)
            best = Math.Min(best, applicable.Min());

        // Percentage-discount roles.
        var discounts = await _db.CustomerRoles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id) && r.IsActive
                        && r.PricingRuleType == CustomerRolePricingRuleType.PercentageDiscount
                        && r.DiscountPercent != null && r.DiscountPercent > 0)
            .Select(r => r.DiscountPercent!.Value)
            .ToListAsync(cancellationToken);

        foreach (var percent in discounts)
        {
            var discounted = basePrice * (1 - Math.Min(percent, 100m) / 100m);
            best = Math.Min(best, discounted);
        }

        return Math.Round(best, 2);
    }

    public async Task<bool> IsProductAccessibleAsync(Guid productId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var restrictions = await _db.ProductRoleRestrictions
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .Select(r => r.CustomerRoleId)
            .ToListAsync(cancellationToken);

        return restrictions.Count == 0 || restrictions.Any(roleIds.Contains);
    }

    public async Task<bool> IsCategoryAccessibleAsync(Guid categoryId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var restrictions = await _db.CategoryRoleRestrictions
            .AsNoTracking()
            .Where(r => r.CategoryId == categoryId)
            .Select(r => r.CustomerRoleId)
            .ToListAsync(cancellationToken);

        return restrictions.Count == 0 || restrictions.Any(roleIds.Contains);
    }
}
