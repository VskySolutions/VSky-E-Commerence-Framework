using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using WishlistEntity = VSky.Domain.Entities.Wishlist;

namespace VSky.Application.Features.Wishlist;

/// <summary>
/// Shared wishlist-resolution and projection logic (REQ-CHK-002). A wishlist belongs to the
/// authenticated customer; there is exactly one active wishlist per customer, resolved from their
/// identity. Static helper (no DI) so each handler injects only what it needs.
/// </summary>
internal static class WishlistResolver
{
    /// <summary>Finds the current customer's wishlist, creating (tracked, unsaved) an empty one if none exists.</summary>
    public static async Task<WishlistEntity> ResolveOrCreateAsync(
        IApplicationDbContext db, ICurrentUserService current, CancellationToken ct)
    {
        var customerId = await RequireCustomerIdAsync(db, current, ct);

        var wishlist = await db.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.CustomerId == customerId, ct);

        if (wishlist is not null)
            return wishlist;

        wishlist = new WishlistEntity { CustomerId = customerId };
        db.Wishlists.Add(wishlist);
        return wishlist;
    }

    /// <summary>Finds the current customer's wishlist, throwing <see cref="NotFoundException"/> if none exists.</summary>
    public static async Task<WishlistEntity> ResolveExistingAsync(
        IApplicationDbContext db, ICurrentUserService current, CancellationToken ct)
    {
        var customerId = await RequireCustomerIdAsync(db, current, ct);

        return await db.Wishlists
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.CustomerId == customerId, ct)
            ?? throw new NotFoundException("No wishlist exists for the current customer.");
    }

    private static async Task<Guid> RequireCustomerIdAsync(IApplicationDbContext db, ICurrentUserService current, CancellationToken ct)
    {
        if (current.UserId is not Guid userId)
            throw new UnauthorizedException("Authentication is required to use a wishlist.");

        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

        return customer.Id;
    }

    /// <summary>Projects a wishlist into a DTO, resolving each item against the live catalog (newest first).</summary>
    public static async Task<WishlistDto> BuildDtoAsync(IApplicationDbContext db, WishlistEntity wishlist, CancellationToken ct)
    {
        var items = wishlist.Items.OrderByDescending(i => i.CreatedOnUtc).ToList();

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var variantIds = items.Where(i => i.ProductVariantId.HasValue)
            .Select(i => i.ProductVariantId!.Value).Distinct().ToList();

        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : await db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var variants = variantIds.Count == 0
            ? new Dictionary<Guid, ProductVariant>()
            : await db.ProductVariants.AsNoTracking().Where(v => variantIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, ct);

        var itemDtos = new List<WishlistItemDto>(items.Count);
        foreach (var item in items)
        {
            products.TryGetValue(item.ProductId, out var product);
            ProductVariant? variant = null;
            if (item.ProductVariantId is Guid variantId)
                variants.TryGetValue(variantId, out variant);

            if (product is null)
            {
                itemDtos.Add(WishlistItemDto.From(item, "Unavailable product", null, 0m, available: false));
                continue;
            }

            var name = product.Name;
            var sku = variant?.Sku ?? product.Sku;
            var price = variant?.Price ?? product.Price ?? 0m;
            var available = product.IsPublished
                && (!item.ProductVariantId.HasValue || (variant is not null && variant.IsEnabled));

            itemDtos.Add(WishlistItemDto.From(item, name, sku, price, available));
        }

        return WishlistDto.From(wishlist, itemDtos);
    }
}
