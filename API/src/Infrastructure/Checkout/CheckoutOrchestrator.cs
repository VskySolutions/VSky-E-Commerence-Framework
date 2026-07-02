using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VSky.Application.Common.Exceptions;
using VSky.Application.Common.Interfaces;
using VSky.Application.Common.Models;
using VSky.Application.Features.Checkout;
using VSky.Application.Features.OrderRouting;
using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Infrastructure.Checkout;

/// <summary>
/// The checkout capstone (WO-30, REQ-CHK-003). Wires the already-built commerce services into the
/// canonical checkout sequence: load cart → route to a store → enforce guest-ordering policy → quote
/// shipping → evaluate discounts/coupons → calculate tax → (on placement) create the order, authorize
/// payment, and finalize. Pricing math is shared between <see cref="QuoteAsync"/> and
/// <see cref="PlaceAsync"/> via <see cref="BuildAsync"/>, so a quote and its placement agree.
/// </summary>
public class CheckoutOrchestrator : ICheckoutOrchestrator
{
    private readonly IApplicationDbContext _db;
    private readonly IOrderRoutingEngine _routing;
    private readonly IShippingRateService _shipping;
    private readonly IDiscountService _discounts;
    private readonly ICouponService _coupons;
    private readonly ITaxCalculationService _tax;
    private readonly IPaymentGatewayRouter _payments;
    private readonly ICurrentUserService _current;
    private readonly IDateTimeProvider _clock;
    private readonly IEmailEnqueuer _emails;
    private readonly IPublisher _publisher;

    public CheckoutOrchestrator(
        IApplicationDbContext db,
        IOrderRoutingEngine routing,
        IShippingRateService shipping,
        IDiscountService discounts,
        ICouponService coupons,
        ITaxCalculationService tax,
        IPaymentGatewayRouter payments,
        ICurrentUserService current,
        IDateTimeProvider clock,
        IEmailEnqueuer emails,
        IPublisher publisher)
    {
        _db = db;
        _routing = routing;
        _shipping = shipping;
        _discounts = discounts;
        _coupons = coupons;
        _tax = tax;
        _payments = payments;
        _current = current;
        _clock = clock;
        _emails = emails;
        _publisher = publisher;
    }

    public async Task<CheckoutQuote> QuoteAsync(CheckoutQuoteRequest req, CancellationToken ct)
    {
        var priced = await BuildAsync(
            req.CartId, req.SessionId, req.ShipTo, req.ShippingMethodId,
            requestCouponCode: null, forPlacement: false, ct);

        return new CheckoutQuote
        {
            Subtotal = priced.Subtotal,
            Discounts = priced.Discounts,
            DiscountTotal = priced.DiscountTotal,
            ShippingOptions = priced.ShippingOptions.ToList(),
            ShippingTotal = priced.ShippingTotal,
            Tax = priced.Tax,
            TaxTotal = priced.TaxTotal,
            Total = priced.Total,
            AssignedStoreId = priced.Routing.AssignedStoreId,
            IsRoutable = priced.Routing.IsRouted,
            GuestOrderingAllowed = priced.GuestOrderingAllowed,
        };
    }

    public async Task<CheckoutResult> PlaceAsync(PlaceCheckoutRequest req, CancellationToken ct)
    {
        var priced = await BuildAsync(
            req.CartId, req.SessionId, req.ShipTo, req.SelectedShippingMethodId,
            requestCouponCode: req.CouponCode, forPlacement: true, ct);

        var now = _clock.UtcNow;
        var storeId = priced.Routing.AssignedStoreId!.Value; // non-null: placement short-circuits when unrouted.

        // Resolve the customer only when the caller is an authenticated customer; guests place a null-customer order.
        Guid? customerId = null;
        if (_current.UserId is Guid userId)
        {
            customerId = await _db.Customers
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        var contactName = $"{req.ShipTo.FirstName} {req.ShipTo.LastName}".Trim();

        // 8a. Create the order + line snapshots + the initial status-history marker (Pending → Pending).
        var order = new Order
        {
            OrderNumber = $"ORD-{now:yyyyMMddHHmmssfff}",
            Status = OrderStatus.Pending,
            CustomerId = customerId,
            ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName,
            ContactEmail = req.ShipTo.Email,
            Latitude = req.ShipTo.Latitude,
            Longitude = req.ShipTo.Longitude,
            CountryCode = req.ShipTo.CountryCode,
            Region = req.ShipTo.Region,
            PostalCode = req.ShipTo.PostalCode,
            AddressLine1 = req.ShipTo.Line1,
            AddressLine2 = req.ShipTo.Line2,
            City = req.ShipTo.City,
            StateProvince = req.ShipTo.Region,
            AssignedStoreId = storeId,
            PlacedOnUtc = now,
            RoutedOnUtc = now,
            CurrencyCode = priced.Cart.CurrencyCode,
            Subtotal = priced.Subtotal,
            DiscountTotal = priced.DiscountTotal,
            ShippingTotal = priced.ShippingTotal,
            TaxTotal = priced.TaxTotal,
            TotalAmount = priced.Total,
            // Immutable jurisdiction-level tax breakdown, stored verbatim and never recalculated.
            TaxBreakdownJson = JsonSerializer.Serialize(priced.Tax),
            TaxFlaggedForReview = priced.Tax.FallbackApplied,
            AppliedCouponCode = priced.CouponValid ? priced.CouponCode : null,
            ShippingMethodName = priced.SelectedShipping?.Name,
            ShippingCarrier = priced.SelectedShipping?.Carrier,
        };

        foreach (var line in priced.Lines)
        {
            order.Lines.Add(new OrderLineItem
            {
                ProductId = line.ProductId,
                ProductVariantId = line.ProductVariantId,
                ProductName = line.ProductName,
                Sku = line.Sku,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = line.LineTotal,
            });
        }

        order.StatusHistory.Add(new OrderStatusHistory
        {
            FromStatus = OrderStatus.Pending,
            ToStatus = OrderStatus.Pending,
            ChangedById = _current.UserId,
            ChangedOnUtc = now,
            Note = "Order created via checkout.",
        });

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        // 8b. Create a payment record and authorize it through the gateway router (applies the gateway's capture mode).
        var payment = new PaymentRecord
        {
            OrderId = order.Id,
            Method = req.PaymentMethod,
            GatewayName = req.PaymentMethod.ToString(),
            Amount = priced.Total,
            CurrencyCode = order.CurrencyCode,
            Status = PaymentStatus.Pending,
        };
        _db.PaymentRecords.Add(payment);

        var payResult = await _payments.AuthorizeAsync(
            new PaymentRequest(order.Id, req.PaymentMethod, priced.Total, order.CurrencyCode, req.PaymentToken),
            ct);

        payment.Status = payResult.Status;
        payment.AuthorizationId = payResult.AuthorizationId;
        payment.TransactionId = payResult.TransactionId;
        payment.GatewayReference = payResult.GatewayReference;
        payment.ErrorMessage = payResult.ErrorMessage;
        if (payResult.Status == PaymentStatus.Captured)
        {
            payment.AuthorizedOnUtc = now;
            payment.CapturedOnUtc = now;
        }
        else if (payResult.Status == PaymentStatus.Authorized)
        {
            payment.AuthorizedOnUtc = now;
        }

        // Mirror the payment state onto the order for at-a-glance status.
        order.PaymentStatus = payResult.Status;
        await _db.SaveChangesAsync(ct);

        // Payment failed: leave the order Pending/unpaid so the buyer can retry, and do NOT mark the
        // cart checked out or fire any confirmation side effects (AC-PAY-001.3).
        if (!payResult.Success)
        {
            return new CheckoutResult
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status.ToString(),
                Total = priced.Total,
                PaymentStatus = payResult.Status.ToString(),
                Success = false,
                Error = payResult.ErrorMessage
                        ?? "Payment could not be processed. Please try again or choose another payment method.",
            };
        }

        // 8c. Payment succeeded: finalize the cart, redeem the coupon, raise events, and send the confirmation email.
        priced.Cart.IsCheckedOut = true;
        await _db.SaveChangesAsync(ct);

        if (priced.CouponValid && priced.CouponCode is not null)
            await _coupons.RedeemAsync(priced.CouponCode, ct);

        await _publisher.Publish(
            new OrderPlaced(order.Id, order.OrderNumber, priced.Total, order.CustomerId, order.ContactEmail),
            ct);
        await _publisher.Publish(
            new OrderRouted(BuildRoutingRequest(req.ShipTo, priced.Lines), storeId),
            ct);

        // AC-CHK-003.8: queue an order-confirmation email with an inline subject/body carrying the order number.
        await _emails.EnqueueAsync(
            "OrderConfirmation",
            req.ShipTo.Email,
            string.IsNullOrWhiteSpace(contactName) ? null : contactName,
            $"Your order {order.OrderNumber} is confirmed",
            $"Hi {req.ShipTo.FirstName},\n\n" +
            $"Thank you for your order. We've received order {order.OrderNumber} and it is now being processed.\n\n" +
            $"Order total: {order.CurrencyCode} {priced.Total:0.00}\n\n" +
            "You'll receive another email once your order ships.\n\n" +
            "Thank you for shopping with us.",
            cancellationToken: ct);

        return new CheckoutResult
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            Total = priced.Total,
            PaymentStatus = payResult.Status.ToString(),
            Success = true,
            Error = null,
        };
    }

    /// <summary>
    /// Runs the shared pricing sequence (steps 1–7) and returns every computed part. For a placement,
    /// the routing-failure and guest-ordering checks are hard stops (throw); for a quote they are
    /// surfaced softly on the returned <see cref="PricedCheckout"/> so the preview can still render.
    /// Multi-address shipping (AC-CHK-003.4) is a future enhancement: a single ship-to address is
    /// priced as one shipment here; splitting a cart across addresses/stores would fan this out per group.
    /// </summary>
    private async Task<PricedCheckout> BuildAsync(
        Guid? cartId, string? sessionId, CheckoutAddress shipTo, string? shippingMethodId,
        string? requestCouponCode, bool forPlacement, CancellationToken ct)
    {
        // 1. Load the cart + its lines and compute the subtotal from the snapshotted unit prices.
        var cart = await LoadCartAsync(cartId, sessionId, ct);
        var items = cart.Items.OrderBy(i => i.CreatedOnUtc).ToList();
        if (items.Count == 0)
            throw new ConflictException("The cart is empty. Add items before checking out.");

        var lines = await BuildLinesAsync(items, ct);
        var subtotal = lines.Sum(l => l.LineTotal);

        // 2. Route the order against active stores (ship-to address + line items).
        var routing = await _routing.RouteAsync(BuildRoutingRequest(shipTo, lines), ct);

        if (!routing.IsRouted || routing.AssignedStoreId is not Guid storeId)
        {
            if (forPlacement)
                throw new ConflictException(
                    "This order cannot be fulfilled: no eligible store could be found for the delivery address.");

            // Quote: report the subtotal but flag the cart as not routable; shipping/tax need a store.
            return new PricedCheckout(
                cart, lines, subtotal, routing, Store: null,
                ShippingOptions: Array.Empty<ShippingRateOption>(), SelectedShipping: null, ShippingTotal: 0m,
                Discounts: new List<AppliedDiscount>(), DiscountTotal: 0m,
                Tax: new TaxBreakdown(0m, new(), false), TaxTotal: 0m,
                Total: subtotal, GuestOrderingAllowed: true,
                CouponCode: cart.AppliedCouponCode, CouponValid: false);
        }

        // 3. Guest-ordering policy (AC-STR-001.5 / AC-CHK-003.2): load the assigned store and, if it
        // forbids guest ordering, require an authenticated buyer. A placement is rejected with a 401;
        // a quote merely reports GuestOrderingAllowed = false so the UI can prompt login/registration.
        var store = await _db.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == storeId, ct)
            ?? throw new ConflictException("The assigned fulfilment store could not be loaded.");

        var isAuthenticated = _current.UserId is not null;
        var guestOrderingAllowed = store.GuestOrderingEnabled || isAuthenticated;
        if (forPlacement && !guestOrderingAllowed)
            throw new UnauthorizedException(
                "Guest checkout is not available for this order. Please log in or create an account to continue.");

        // 4. Shipping: origin = assigned store, destination = ship-to. No per-product weight exists in the
        // catalog, so weight is a best-effort proxy (total units, min 1 kg).
        var weightKg = Math.Max(1m, lines.Sum(l => (decimal)l.Quantity));
        var shippingOptions = await _shipping.GetRatesAsync(
            new CarrierRateRequest(
                new CarrierAddress(store.CountryCode, store.StateProvince, store.PostalCode, store.Latitude, store.Longitude),
                new CarrierAddress(shipTo.CountryCode, shipTo.Region, shipTo.PostalCode, shipTo.Latitude, shipTo.Longitude),
                weightKg, Length: null, Width: null, Height: null, OrderSubtotal: subtotal),
            ct);

        ShippingRateOption? selected;
        if (forPlacement)
        {
            // The buyer's chosen method must still be on offer.
            selected = shippingOptions.FirstOrDefault(o => o.MethodId == shippingMethodId)
                ?? throw new ConflictException(
                    $"The selected shipping method '{shippingMethodId}' is no longer available. Please choose another option.");
        }
        else
        {
            // Prefer the requested method if given and still offered; otherwise default to the cheapest.
            selected = !string.IsNullOrWhiteSpace(shippingMethodId)
                ? shippingOptions.FirstOrDefault(o => o.MethodId == shippingMethodId)
                : null;
            selected ??= shippingOptions.OrderBy(o => o.Rate).FirstOrDefault();
        }
        var shippingTotal = selected?.Rate ?? 0m;

        // 5. Discounts: the discount engine evaluates every active, in-window rule (including coupon-bound
        // ones), so a valid coupon's discount is already reflected in the result. Validating the coupon
        // here gates redemption and records the applied code on the order.
        var discountLines = lines
            .Select(l => new DiscountCartLine(l.ProductId, l.CategoryIds, l.LineTotal, l.Quantity))
            .ToList();
        var discountResult = await _discounts.EvaluateAsync(discountLines, subtotal, ct);

        var couponCode = FirstNonEmpty(requestCouponCode, cart.AppliedCouponCode);
        var couponValid = false;
        if (couponCode is not null)
        {
            var validation = await _coupons.ValidateAsync(couponCode, ct);
            couponValid = validation.IsValid;
            // An invalid coupon is ignored (not applied, not recorded) so checkout stays resilient;
            // cart-applied coupons were already validated at apply time.
        }

        // 6. Tax (AC-CHK-003.5): origin = store address, destination = ship-to, plus the taxable shipping cost.
        var tax = await _tax.CalculateAsync(
            new TaxCalculationRequest(
                new TaxAddress(store.CountryCode ?? string.Empty, store.StateProvince, store.PostalCode, store.City),
                new TaxAddress(shipTo.CountryCode, shipTo.Region, shipTo.PostalCode, shipTo.City),
                lines.Select(l => new TaxLineInput(l.ProductId, l.TaxCategoryCode, l.UnitPrice, l.Quantity)).ToList(),
                shippingTotal),
            ct);

        // 7. Grand total.
        var total = subtotal - discountResult.TotalDiscount + shippingTotal + tax.TotalTax;

        return new PricedCheckout(
            cart, lines, subtotal, routing, store,
            shippingOptions, selected, shippingTotal,
            discountResult.Applied, discountResult.TotalDiscount,
            tax, tax.TotalTax, total, guestOrderingAllowed, couponCode, couponValid);
    }

    /// <summary>
    /// Resolves the cart to check out: by explicit <paramref name="cartId"/>, else the authenticated
    /// customer's active cart, else a guest cart keyed by <paramref name="sessionId"/>. Mirrors the Cart
    /// feature's resolution rules (a customer's cart is restored across sessions). Checked-out carts are
    /// excluded so a completed cart is never re-used.
    /// </summary>
    private async Task<Cart> LoadCartAsync(Guid? cartId, string? sessionId, CancellationToken ct)
    {
        if (cartId is Guid id)
        {
            return await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsCheckedOut, ct)
                ?? throw new NotFoundException("No active cart exists for the supplied cart id.");
        }

        if (_current.UserId is Guid userId)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId, ct)
                ?? throw new ForbiddenAccessException("The current user does not have a customer profile.");

            return await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && !c.IsCheckedOut, ct)
                ?? throw new NotFoundException("No active cart exists for the current user.");
        }

        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ConflictException(
                "A cart could not be resolved. Provide a cart id or a guest session id, or sign in.");

        var session = sessionId.Trim();
        return await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == session && c.CustomerId == null && !c.IsCheckedOut, ct)
            ?? throw new NotFoundException("No active cart exists for the supplied session id.");
    }

    /// <summary>
    /// Projects the cart lines into working rows enriched from the live catalog: product name/SKU (for the
    /// order snapshot), the product's category ids (for category-scoped discounts) and its tax-category
    /// code (for tax). The line's unit price is the cart's snapshot, so pricing is stable across the flow.
    /// </summary>
    private async Task<List<LineWork>> BuildLinesAsync(List<CartItem> items, CancellationToken ct)
    {
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var variantIds = items.Where(i => i.ProductVariantId.HasValue)
            .Select(i => i.ProductVariantId!.Value).Distinct().ToList();

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var variants = variantIds.Count == 0
            ? new Dictionary<Guid, ProductVariant>()
            : await _db.ProductVariants
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, ct);

        var categoriesByProduct = (await _db.ProductCategories
                .AsNoTracking()
                .Where(pc => productIds.Contains(pc.ProductId))
                .Select(pc => new { pc.ProductId, pc.CategoryId })
                .ToListAsync(ct))
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(x => x.CategoryId).ToList());

        var taxCategoryIds = products.Values.Select(p => p.TaxCategoryId).Distinct().ToList();
        var taxCategoryCodes = taxCategoryIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.TaxCategories
                .AsNoTracking()
                .Where(t => taxCategoryIds.Contains(t.Id))
                // No dedicated provider-code column exists; the Tax Category name is the provider tax code.
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var lines = new List<LineWork>(items.Count);
        foreach (var item in items)
        {
            products.TryGetValue(item.ProductId, out var product);
            ProductVariant? variant = null;
            if (item.ProductVariantId is Guid variantId)
                variants.TryGetValue(variantId, out variant);

            var name = product?.Name ?? "Unavailable product";
            var sku = variant?.Sku ?? product?.Sku;

            string? taxCode = null;
            if (product is not null)
                taxCategoryCodes.TryGetValue(product.TaxCategoryId, out taxCode);

            var categoryIds = categoriesByProduct.TryGetValue(item.ProductId, out var cats)
                ? cats
                : Array.Empty<Guid>();

            lines.Add(new LineWork(
                item.ProductId, item.ProductVariantId, item.Quantity, item.UnitPrice,
                name, sku, taxCode, categoryIds));
        }

        return lines;
    }

    private static RoutingRequest BuildRoutingRequest(CheckoutAddress shipTo, IReadOnlyList<LineWork> lines) =>
        new(shipTo.Latitude, shipTo.Longitude, shipTo.CountryCode, shipTo.Region, shipTo.PostalCode,
            lines.Select(l => new RoutingLineItem(l.ProductId, l.ProductVariantId, l.Quantity)).ToList());

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        return null;
    }

    /// <summary>A cart line enriched with the catalog data the checkout sequence needs.</summary>
    private sealed record LineWork(
        Guid ProductId,
        Guid? ProductVariantId,
        int Quantity,
        decimal UnitPrice,
        string ProductName,
        string? Sku,
        string? TaxCategoryCode,
        IReadOnlyList<Guid> CategoryIds)
    {
        public decimal LineTotal => UnitPrice * Quantity;
    }

    /// <summary>The full set of computed parts shared between a quote and its placement.</summary>
    private sealed record PricedCheckout(
        Cart Cart,
        List<LineWork> Lines,
        decimal Subtotal,
        RoutingResult Routing,
        Store? Store,
        IReadOnlyList<ShippingRateOption> ShippingOptions,
        ShippingRateOption? SelectedShipping,
        decimal ShippingTotal,
        List<AppliedDiscount> Discounts,
        decimal DiscountTotal,
        TaxBreakdown Tax,
        decimal TaxTotal,
        decimal Total,
        bool GuestOrderingAllowed,
        string? CouponCode,
        bool CouponValid);
}
