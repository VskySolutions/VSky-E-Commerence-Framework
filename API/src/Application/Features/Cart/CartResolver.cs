using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Domain.Entities;
using CartEntity = VSky.Domain.Entities.Cart;
using CartItemEntity = VSky.Domain.Entities.CartItem;

namespace VSky.Application.Features.Cart;

/// <summary>
/// Shared cart-resolution and projection logic for the Cart feature (REQ-CHK-001). An authenticated
/// caller's cart is keyed by their <see cref="Customer"/> — so it is restored on login (AC-CHK-001.4);
/// a guest's cart is keyed by a client-supplied session id (a <c>sessionId</c> query/body parameter or
/// the <c>X-Cart-Session</c> header). Kept as a static helper so it needs no DI registration and each
/// MediatR handler can inject only <see cref="IApplicationDbContext"/> and <see cref="ICurrentUserService"/>.
/// </summary>
internal static class CartResolver
{
    /// <summary>Finds the caller's active cart, creating (and tracking, not yet saved) an empty one if none exists.</summary>
    public static Task<CartEntity> ResolveOrCreateAsync(
        IApplicationDbContext db, ICurrentUserService current, string? sessionId, CancellationToken ct)
        => ResolveAsync(db, current, sessionId, create: true, ct);

    /// <summary>Finds the caller's active cart, throwing <see cref="NotFoundException"/> if none exists.</summary>
    public static Task<CartEntity> ResolveExistingAsync(
        IApplicationDbContext db, ICurrentUserService current, string? sessionId, CancellationToken ct)
        => ResolveAsync(db, current, sessionId, create: false, ct);

    private static async Task<CartEntity> ResolveAsync(
        IApplicationDbContext db, ICurrentUserService current, string? sessionId, bool create, CancellationToken ct)
    {
        // Authenticated buyer: resolve the cart through the customer profile so it is persisted and
        // restored across sessions/devices (AC-CHK-001.4).
        if (current.UserId is Guid userId)
        {
            var customer = await db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId, ct)
                ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

            var cart = await db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && !c.IsCheckedOut, ct);

            if (cart is not null)
                return cart;
            if (!create)
                throw new NotFoundException("No active cart exists for the current user.");

            cart = new CartEntity { CustomerId = customer.Id };
            db.Carts.Add(cart);
            return cart;
        }

        // Guest: resolve by the client-supplied session id.
        var session = NormalizeGuestSession(sessionId);

        var guestCart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == session && c.CustomerId == null && !c.IsCheckedOut, ct);

        if (guestCart is not null)
            return guestCart;
        if (!create)
            throw new NotFoundException("No active cart exists for the supplied session id.");

        guestCart = new CartEntity { SessionId = session };
        db.Carts.Add(guestCart);
        return guestCart;
    }

    private static string NormalizeGuestSession(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ValidationException(new[]
            {
                new ValidationFailure("sessionId",
                    "A cart session id is required for guest carts. Provide it via the 'sessionId' query parameter or the 'X-Cart-Session' header."),
            });
        return sessionId.Trim();
    }

    /// <summary>
    /// Projects a resolved cart into a <see cref="CartDto"/>, re-checking each line against the live
    /// catalog: lines whose product/variant is missing, unpublished or out of stock are flagged
    /// <c>Available = false</c> with a matching warning (AC-CHK-001.5), and a line whose quantity exceeds
    /// the available (non-backorder) stock stays available but raises a best-effort warning (AC-CHK-001.6).
    /// </summary>
    public static async Task<CartDto> BuildDtoAsync(IApplicationDbContext db, CartEntity cart, CancellationToken ct)
    {
        var items = cart.Items.OrderBy(i => i.CreatedOnUtc).ToList();

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var variantIds = items.Where(i => i.ProductVariantId.HasValue)
            .Select(i => i.ProductVariantId!.Value).Distinct().ToList();

        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : await db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

        var variants = variantIds.Count == 0
            ? new Dictionary<Guid, ProductVariant>()
            : await db.ProductVariants.AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, ct);

        var itemDtos = new List<CartItemDto>(items.Count);
        var warnings = new List<string>();

        foreach (var item in items)
        {
            products.TryGetValue(item.ProductId, out var product);
            ProductVariant? variant = null;
            if (item.ProductVariantId is Guid variantId)
                variants.TryGetValue(variantId, out variant);

            var (available, name, sku) = Evaluate(item, product, variant, warnings);
            itemDtos.Add(CartItemDto.From(item, name, sku, available));
        }

        return CartDto.From(cart, itemDtos, warnings);
    }

    private static (bool available, string name, string? sku) Evaluate(
        CartItemEntity item, Product? product, ProductVariant? variant, List<string> warnings)
    {
        // Soft-deleted products/variants are excluded by the global query filter, so a null lookup means
        // the catalog entry is gone entirely.
        if (product is null)
        {
            warnings.Add("A product in your cart is no longer available and has been marked unavailable.");
            return (false, "Unavailable product", null);
        }

        var name = product.Name;
        var sku = variant?.Sku ?? product.Sku;

        if (!product.IsPublished)
        {
            warnings.Add($"'{name}' is no longer available and has been marked unavailable.");
            return (false, name, sku);
        }

        if (item.ProductVariantId.HasValue && (variant is null || !variant.IsEnabled))
        {
            warnings.Add($"A selected option for '{name}' is no longer available.");
            return (false, name, sku);
        }

        var stock = variant?.StockQuantity ?? product.StockQuantity;
        var allowBackorder = variant?.AllowBackorder ?? product.AllowBackorder;

        if (stock <= 0 && !allowBackorder)
        {
            warnings.Add($"'{name}' is out of stock.");
            return (false, name, sku);
        }

        // Best-effort quantity check: no explicit per-product max exists, so flag a request that
        // exceeds current stock without blocking the line (AC-CHK-001.6).
        if (!allowBackorder && item.Quantity > stock)
            warnings.Add($"Only {stock} unit(s) of '{name}' are in stock; your cart requests {item.Quantity}.");

        return (true, name, sku);
    }
}
