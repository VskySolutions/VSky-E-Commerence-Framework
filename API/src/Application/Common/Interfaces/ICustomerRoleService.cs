namespace VSky.Application.Common.Interfaces;

/// <summary>
/// Resolves role-based group pricing and catalog-access restrictions for a customer (REQ-CUS-003). The
/// storefront/cart/checkout price and visibility paths call this so members of a pricing role see their
/// adjusted price, and role-restricted content is hidden from non-members (and anonymous callers).
/// </summary>
public interface ICustomerRoleService
{
    /// <summary>The active role ids assigned to a customer (empty for guests).</summary>
    Task<IReadOnlyList<Guid>> GetCustomerRoleIdsAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The best (lowest) price for a product/variant given the caller's roles: the base price, any explicit
    /// group price, and any percentage-discount role are all considered (AC-CUS-003.3).
    /// </summary>
    Task<decimal> ResolvePriceAsync(
        Guid productId, Guid? variantId, decimal basePrice, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>Whether a product is visible to the caller's roles (unrestricted products are visible to all) (AC-CUS-003.4).</summary>
    Task<bool> IsProductAccessibleAsync(Guid productId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>Whether a category is visible to the caller's roles (unrestricted categories are visible to all).</summary>
    Task<bool> IsCategoryAccessibleAsync(Guid categoryId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);
}
