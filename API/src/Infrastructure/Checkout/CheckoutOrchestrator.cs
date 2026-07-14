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
    private readonly IEmailTemplateSender _templates;
    private readonly IPublisher _publisher;
    private readonly ICustomerRoleService _customerRoles;

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
        IEmailTemplateSender templates,
        IPublisher publisher,
        ICustomerRoleService customerRoles)
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
        _templates = templates;
        _publisher = publisher;
        _customerRoles = customerRoles;
    }

    public async Task<CheckoutQuote> QuoteAsync(CheckoutQuoteRequest req, CancellationToken ct)
    {
        var priced = await BuildAsync(
            req.CartId, req.SessionId, req.ShipTo, req.ShippingMethodId,
            requestCouponCode: null, forPlacement: false, req.PickupInStore, req.PickupStoreId, ct);

        var quote = new CheckoutQuote
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

        // Only a routed order has a fulfilling store, and so a concrete set of payment methods to offer.
        if (priced.Store is not null)
            quote.AvailablePaymentMethods = (await AvailablePaymentMethodsAsync(priced.Store, ct))
                .Select(m => new PaymentMethodOption { Method = m.Method.ToString(), IsProduction = m.IsProduction })
                .ToList();

        // The active tax provider (for the storefront's "calculated via …" indicator); defaults to FlatRate
        // when no configuration row exists, mirroring TaxCalculationService.
        quote.TaxProvider = (await _db.TaxProviderConfigurations
            .AsNoTracking()
            .Select(c => (TaxProviderType?)c.ActiveProvider)
            .FirstOrDefaultAsync(ct) ?? TaxProviderType.FlatRate).ToString();

        return quote;
    }

    /// <summary>
    /// Payment methods offered for an order fulfilled by <paramref name="store"/>: the active/enabled +
    /// credential-configured gateways (<see cref="IPaymentGatewayRouter.AvailableMethodsAsync"/>), with Cash
    /// on Delivery included only when the store enables it (<see cref="Domain.Entities.Store.CashOnDeliveryEnabled"/>).
    /// </summary>
    private async Task<IReadOnlyList<PaymentMethodAvailability>> AvailablePaymentMethodsAsync(Domain.Entities.Store store, CancellationToken ct)
    {
        var methods = await _payments.AvailableMethodsAsync(ct);
        return store.CashOnDeliveryEnabled
            ? methods
            : methods.Where(m => m.Method != PaymentMethodType.CashOnDelivery).ToList();
    }

    public async Task<CheckoutResult> PlaceAsync(PlaceCheckoutRequest req, CancellationToken ct)
    {
        var priced = await BuildAsync(
            req.CartId, req.SessionId, req.ShipTo, req.SelectedShippingMethodId,
            requestCouponCode: req.CouponCode, forPlacement: true, req.PickupInStore, req.PickupStoreId, ct);

        var now = _clock.UtcNow;
        var storeId = priced.Routing.AssignedStoreId!.Value; // non-null: placement short-circuits when unrouted.

        // Enforce the store's payment-method availability (e.g. COD switched off, or a deactivated gateway)
        // server-side — the storefront only ever offers these, but never trust the client.
        if (priced.Store is not null && !(await AvailablePaymentMethodsAsync(priced.Store, ct)).Any(m => m.Method == req.PaymentMethod))
            throw new ConflictException("The selected payment method is not available for this order.");

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

        // 8a. Create the order + its shared delivery Address + line snapshots + the initial status marker.
        var order = new Order
        {
            OrderNumber = $"ORD-{now:yyyyMMddHHmmssfff}",
            Status = OrderStatus.Pending,
            CustomerId = customerId,
            ShippingAddress = new Address
            {
                FirstName = req.ShipTo.FirstName,
                LastName = req.ShipTo.LastName,
                Email = req.ShipTo.Email,
                PhoneNumber = req.ShipTo.PhoneNumber,
                Latitude = req.ShipTo.Latitude,
                Longitude = req.ShipTo.Longitude,
                CountryCode = req.ShipTo.CountryCode ?? string.Empty,
                StateProvince = req.ShipTo.Region,
                PostalCode = req.ShipTo.PostalCode ?? string.Empty,
                AddressLine1 = req.ShipTo.Line1 ?? string.Empty,
                AddressLine2 = req.ShipTo.Line2,
                Landmark = req.ShipTo.Landmark,
                City = req.ShipTo.City ?? string.Empty,
            },
            AssignedStoreId = storeId,
            IsPickup = priced.IsPickup,
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
            // Provider calculation reference (e.g. Stripe Tax calculation id) for post-completion reporting (WO-37).
            TaxProviderCalculationRef = priced.Tax.ProviderReference,
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
            new PaymentRequest(order.Id, req.PaymentMethod, priced.Total, order.CurrencyCode, req.PaymentToken,
                OrderNumber: order.OrderNumber),
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

        // Redirect gateway (Stripe Checkout): the order is created but stays pending while the buyer pays
        // off-site. Remember the source cart so /api/checkout/confirm can consume it once payment succeeds,
        // and hand back the URL for the storefront to redirect to. Nothing is finalized yet.
        if (payResult.RedirectUrl is not null)
        {
            order.SourceCartId = priced.Cart.Id;
            await _db.SaveChangesAsync(ct);
            return OrderResult(order, success: false, error: null, redirectUrl: payResult.RedirectUrl);
        }

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

        // 8c. Payment succeeded (synchronous gateway): consume the cart and finalize the order.
        priced.Cart.IsCheckedOut = true;
        await _db.SaveChangesAsync(ct);
        await FinalizePlacedOrderAsync(order, ct);

        return OrderResult(order, success: true, error: null, redirectUrl: null, transactionId: payment.TransactionId);
    }

    public async Task<CheckoutResult> ConfirmAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .Include(o => o.ShippingAddress)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(Order), orderId);

        // Idempotent: already confirmed (e.g. the buyer refreshed the return page or double-confirmed).
        if (order.PaymentStatus == PaymentStatus.Captured)
            return OrderResult(order, success: true, error: null, redirectUrl: null,
                transactionId: await CapturedTransactionIdAsync(orderId, ct));

        var payment = await _db.PaymentRecords
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedOnUtc)
            .FirstOrDefaultAsync(ct);
        if (payment is null)
            return OrderResult(order, success: false, error: "No payment to confirm for this order.", redirectUrl: null);

        // Ask the gateway whether the off-site payment actually completed (Stripe session paid?).
        var result = await _payments.VerifyRedirectAsync(payment, ct);
        if (!result.Success || result.Status != PaymentStatus.Captured)
        {
            payment.ErrorMessage = result.ErrorMessage;
            await _db.SaveChangesAsync(ct);
            return OrderResult(order, success: false,
                error: result.ErrorMessage ?? "Payment was not completed. Please try again.", redirectUrl: null);
        }

        var now = _clock.UtcNow;
        payment.Status = PaymentStatus.Captured;
        payment.TransactionId = result.TransactionId ?? payment.TransactionId;
        payment.GatewayReference = result.GatewayReference ?? payment.GatewayReference;
        payment.AuthorizedOnUtc ??= now;
        payment.CapturedOnUtc = now;
        payment.ErrorMessage = null;
        order.PaymentStatus = PaymentStatus.Captured;

        // Consume the source cart now that payment succeeded — kept intact until now so a cancelled
        // payment leaves the buyer's cart untouched for a retry.
        if (order.SourceCartId is Guid cartId)
        {
            var cart = await _db.Carts.FirstOrDefaultAsync(c => c.Id == cartId && !c.IsCheckedOut, ct);
            if (cart is not null)
                cart.IsCheckedOut = true;
        }
        await _db.SaveChangesAsync(ct);

        await FinalizePlacedOrderAsync(order, ct);

        return OrderResult(order, success: true, error: null, redirectUrl: null, transactionId: payment.TransactionId);
    }

    public async Task<CheckoutResult> RetryPaymentAsync(Guid orderId, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(Order), orderId);

        // Already paid — nothing to retry.
        if (order.PaymentStatus == PaymentStatus.Captured)
            return OrderResult(order, success: true, error: null, redirectUrl: null,
                transactionId: await CapturedTransactionIdAsync(orderId, ct));

        var payment = await _db.PaymentRecords
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedOnUtc)
            .FirstOrDefaultAsync(ct);
        var method = payment?.Method ?? PaymentMethodType.Stripe;

        // Re-open a payment session for the same pending order.
        var payResult = await _payments.AuthorizeAsync(
            new PaymentRequest(order.Id, method, order.TotalAmount, order.CurrencyCode, null,
                OrderNumber: order.OrderNumber), ct);

        if (payResult.RedirectUrl is not null)
        {
            if (payment is not null)
            {
                payment.GatewayReference = payResult.GatewayReference;
                payment.Status = PaymentStatus.Pending;
                payment.ErrorMessage = null;
                await _db.SaveChangesAsync(ct);
            }
            return OrderResult(order, success: false, error: null, redirectUrl: payResult.RedirectUrl);
        }

        return OrderResult(order, success: false,
            error: payResult.ErrorMessage ?? "Could not start payment. Please try again.", redirectUrl: null);
    }

    /// <summary>
    /// The side-effects raised once an order is both placed AND paid: redeem the coupon, publish the
    /// order-placed/routed events, and send the confirmation + store-notification emails. Reconstructed
    /// entirely from the persisted order so it runs identically inline (synchronous gateways) or on the
    /// buyer's return (redirect gateways). The order's <see cref="Order.ShippingAddress"/> and
    /// <see cref="Order.Lines"/> must be loaded.
    /// </summary>
    private async Task FinalizePlacedOrderAsync(Order order, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(order.AppliedCouponCode))
            await _coupons.RedeemAsync(order.AppliedCouponCode!, ct);

        await _publisher.Publish(
            new OrderPlaced(order.Id, order.OrderNumber, order.TotalAmount, order.CustomerId, order.ContactEmail),
            ct);

        if (order.AssignedStoreId is Guid routedStoreId)
        {
            var addr = order.ShippingAddress;
            var routing = new RoutingRequest(
                addr?.Latitude, addr?.Longitude, addr?.CountryCode, addr?.StateProvince, addr?.PostalCode,
                order.Lines.Select(l => new RoutingLineItem(l.ProductId, l.ProductVariantId, l.Quantity)).ToList());
            await _publisher.Publish(new OrderRouted(routing, routedStoreId), ct);
        }

        // AC-CHK-003.8: order-confirmation email to the customer, plus a new-order notification to the
        // assigned store — both rendered from admin-editable email templates.
        var shipTo = order.ShippingAddress;
        var contactName = $"{shipTo?.FirstName} {shipTo?.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(contactName) ? "there" : contactName;
        var orderTotalText = $"{order.CurrencyCode} {order.TotalAmount:0.00}";
        var orderDateText = order.PlacedOnUtc.ToString("MMM d, yyyy");
        var toEmail = order.ContactEmail;

        if (!string.IsNullOrWhiteSpace(toEmail))
        {
            await _templates.SendAsync(
                "order.confirmation",
                toEmail,
                string.IsNullOrWhiteSpace(contactName) ? null : contactName,
                new Dictionary<string, string>
                {
                    ["customerName"] = displayName,
                    ["orderNumber"] = order.OrderNumber,
                    ["orderDate"] = orderDateText,
                    ["orderTotal"] = orderTotalText,
                },
                ct);
        }

        if (order.AssignedStoreId is Guid notifyStoreId)
        {
            var store = await _db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == notifyStoreId, ct);
            if (store is not null)
            {
                var storeVars = new Dictionary<string, string>
                {
                    ["orderNumber"] = order.OrderNumber,
                    ["storeName"] = store.Name,
                    ["customerName"] = displayName,
                    ["customerEmail"] = toEmail ?? string.Empty,
                    ["orderTotal"] = orderTotalText,
                    ["orderDate"] = orderDateText,
                };
                foreach (var to in ResolveStoreRecipients(store))
                    await _templates.SendAsync("order.store-notification", to, store.Name, storeVars, ct);
            }
        }
    }

    /// <summary>Projects an order into the checkout result shape (shared by place/confirm/retry).</summary>
    private static CheckoutResult OrderResult(
        Order order, bool success, string? error, string? redirectUrl, string? transactionId = null)
        => new()
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            Total = order.TotalAmount,
            PaymentStatus = order.PaymentStatus.ToString(),
            Success = success,
            Error = error,
            TransactionId = transactionId,
            RedirectUrl = redirectUrl,
        };

    /// <summary>The gateway transaction id of an order's captured payment (for the confirmation screen), if any.</summary>
    private Task<string?> CapturedTransactionIdAsync(Guid orderId, CancellationToken ct)
        => _db.PaymentRecords
            .AsNoTracking()
            .Where(p => p.OrderId == orderId && p.Status == PaymentStatus.Captured)
            .OrderByDescending(p => p.CapturedOnUtc)
            .Select(p => p.TransactionId)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Recipients for a store's new-order alert: the store's NotificationEmail (one or more addresses
    /// separated by comma/semicolon), falling back to its ContactEmail when the notification field is blank.
    /// </summary>
    private static IEnumerable<string> ResolveStoreRecipients(Domain.Entities.Store store)
    {
        var raw = string.IsNullOrWhiteSpace(store.NotificationEmail) ? store.ContactEmail : store.NotificationEmail;
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        return raw
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);
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
        string? requestCouponCode, bool forPlacement, bool pickupInStore, Guid? pickupStoreId, CancellationToken ct)
    {
        // 1. Load the cart + its lines and compute the subtotal from the snapshotted unit prices.
        var cart = await LoadCartAsync(cartId, sessionId, ct);
        var items = cart.Items.OrderBy(i => i.CreatedOnUtc).ToList();
        if (items.Count == 0)
            throw new ConflictException("The cart is empty. Add items before checking out.");

        var lines = await ApplyGroupPricingAsync(await BuildLinesAsync(items, ct), ct);
        var subtotal = lines.Sum(l => l.LineTotal);

        // Pickup-in-store: skip routing + carrier rates entirely; fulfil at the chosen store (REQ-SHP-004).
        if (pickupInStore)
            return await BuildPickupAsync(cart, lines, subtotal, pickupStoreId, requestCouponCode, forPlacement, ct);

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
        // Include the linked Address: the store's origin country/state/postal/city/lat-long are
        // [NotMapped] read-throughs over Address, and tax (from_country) + carrier rates depend on them.
        var store = await _db.Stores
            .AsNoTracking()
            .Include(s => s.Address)
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
            if (string.IsNullOrWhiteSpace(shippingMethodId))
            {
                // No method chosen — valid only when the store offers no shipping for this order
                // (the storefront enables Place order directly in that case).
                if (shippingOptions.Any())
                    throw new ConflictException("Please choose a shipping method.");
                selected = null;
            }
            else
            {
                // The buyer's chosen method must still be on offer.
                selected = shippingOptions.FirstOrDefault(o => o.MethodId == shippingMethodId)
                    ?? throw new ConflictException(
                        $"The selected shipping method '{shippingMethodId}' is no longer available. Please choose another option.");
            }
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

        // 5. Coupon + discounts. Validate the coupon first so its bound discount is "unlocked" for the
        // engine: coupon-gated rules (RequiresCoupon) apply only when a valid code carrying them is present,
        // while auto-apply rules stack as normal. An invalid coupon is ignored (not applied, not recorded)
        // so checkout stays resilient; cart-applied coupons were already validated at apply time.
        var couponCode = FirstNonEmpty(requestCouponCode, cart.AppliedCouponCode);
        var couponValid = false;
        var unlockedDiscountIds = new List<Guid>();
        if (couponCode is not null)
        {
            var validation = await _coupons.ValidateAsync(couponCode, ct);
            couponValid = validation.IsValid;
            if (validation.IsValid && validation.DiscountId is Guid unlockedId)
                unlockedDiscountIds.Add(unlockedId);
        }

        var discountLines = lines
            .Select(l => new DiscountCartLine(l.ProductId, l.CategoryIds, l.LineTotal, l.Quantity))
            .ToList();
        var discountResult = await _discounts.EvaluateAsync(discountLines, subtotal, unlockedDiscountIds, ct);

        // Tax exemption (AC-TAX-003.3): an authenticated, flagged customer is calculated at zero tax.
        // Resolved here in the shared build path so the quote and the placed order agree. Guests are never exempt.
        TaxExemption? taxExemption = null;
        if (_current.UserId is Guid taxUserId)
        {
            var exempt = await _db.Customers
                .AsNoTracking()
                .Where(c => c.UserId == taxUserId && c.IsTaxExempt)
                .Select(c => new { c.TaxExemptionCertificate, c.VatId })
                .FirstOrDefaultAsync(ct);
            if (exempt is not null)
                taxExemption = new TaxExemption(true, exempt.TaxExemptionCertificate, exempt.VatId);
        }

        // 6. Tax (AC-CHK-003.5): origin = store address, destination = ship-to, plus the taxable shipping cost.
        var tax = await _tax.CalculateAsync(
            new TaxCalculationRequest(
                new TaxAddress(store.CountryCode ?? string.Empty, store.StateProvince, store.PostalCode, store.City),
                new TaxAddress(shipTo.CountryCode, shipTo.Region, shipTo.PostalCode, shipTo.City),
                lines.Select(l => new TaxLineInput(l.ProductId, l.TaxCategoryCode, l.UnitPrice, l.Quantity)).ToList(),
                shippingTotal,
                taxExemption),
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
    /// Prices a pickup-in-store checkout (REQ-SHP-004): no routing/carrier rates — the buyer's chosen store
    /// is the fulfilling store, shipping is free, and tax is collected at the store.
    /// </summary>
    private async Task<PricedCheckout> BuildPickupAsync(
        Cart cart, List<LineWork> lines, decimal subtotal, Guid? pickupStoreId,
        string? requestCouponCode, bool forPlacement, CancellationToken ct)
    {
        if (pickupStoreId is not Guid storeId)
            throw new ConflictException("A pickup store is required for pickup-in-store checkout.");

        var store = await _db.Stores.AsNoTracking().Include(s => s.Address).FirstOrDefaultAsync(s => s.Id == storeId, ct)
            ?? throw new ConflictException("The selected pickup store could not be found.");
        if (!store.IsEnabled || !store.PickupEnabled)
            throw new ConflictException("The selected store does not offer pickup-in-store.");

        var isAuthenticated = _current.UserId is not null;
        var guestOrderingAllowed = store.GuestOrderingEnabled || isAuthenticated;
        if (forPlacement && !guestOrderingAllowed)
            throw new UnauthorizedException(
                "Guest checkout is not available for this order. Please log in or create an account to continue.");

        // A single zero-cost pickup option replaces carrier rates (AC-SHP-004.2).
        var pickupOption = new ShippingRateOption("pickup", "Pickup in store", "Pickup", null, 0m);

        // Coupon + discounts (same engine as delivery): validate first so a coupon-gated discount is
        // unlocked only when its valid code is present.
        var couponCode = FirstNonEmpty(requestCouponCode, cart.AppliedCouponCode);
        var couponValid = false;
        var unlockedDiscountIds = new List<Guid>();
        if (couponCode is not null)
        {
            var validation = await _coupons.ValidateAsync(couponCode, ct);
            couponValid = validation.IsValid;
            if (validation.IsValid && validation.DiscountId is Guid unlockedId)
                unlockedDiscountIds.Add(unlockedId);
        }

        var discountLines = lines
            .Select(l => new DiscountCartLine(l.ProductId, l.CategoryIds, l.LineTotal, l.Quantity))
            .ToList();
        var discountResult = await _discounts.EvaluateAsync(discountLines, subtotal, unlockedDiscountIds, ct);

        // Tax exemption + tax collected at the store (origin = destination = store).
        TaxExemption? taxExemption = null;
        if (_current.UserId is Guid taxUserId)
        {
            var exempt = await _db.Customers
                .AsNoTracking()
                .Where(c => c.UserId == taxUserId && c.IsTaxExempt)
                .Select(c => new { c.TaxExemptionCertificate, c.VatId })
                .FirstOrDefaultAsync(ct);
            if (exempt is not null)
                taxExemption = new TaxExemption(true, exempt.TaxExemptionCertificate, exempt.VatId);
        }

        var storeAddress = new TaxAddress(store.CountryCode ?? string.Empty, store.StateProvince, store.PostalCode, store.City);
        var tax = await _tax.CalculateAsync(
            new TaxCalculationRequest(
                storeAddress, storeAddress,
                lines.Select(l => new TaxLineInput(l.ProductId, l.TaxCategoryCode, l.UnitPrice, l.Quantity)).ToList(),
                0m,
                taxExemption),
            ct);

        var total = subtotal - discountResult.TotalDiscount + tax.TotalTax;
        var addressText = string.Join(", ",
            new[] { store.AddressLine1, store.City, store.PostalCode }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var routing = new RoutingResult(true, storeId, addressText, Array.Empty<StoreEvaluation>());

        return new PricedCheckout(
            cart, lines, subtotal, routing, store,
            new[] { pickupOption }, pickupOption, 0m,
            discountResult.Applied, discountResult.TotalDiscount,
            tax, tax.TotalTax, total, guestOrderingAllowed, couponCode, couponValid, IsPickup: true);
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

            var customerCart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && !c.IsCheckedOut, ct);
            if (customerCart is not null)
                return customerCart;

            // Storefront carts are guest/session-based (built with anonymous cart calls). A logged-in
            // buyer checking out with that session cart resolves it here; the order is still linked to the
            // customer at placement (CustomerId is set from the authenticated user).
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                var authSessionCart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId.Trim() && c.CustomerId == null && !c.IsCheckedOut, ct);
                if (authSessionCart is not null)
                    return authSessionCart;
            }

            throw new NotFoundException("No active cart exists for the current user.");
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

    /// <summary>
    /// Applies the authenticated customer's role-based group pricing to each line (WO-22, AC-CUS-003.3),
    /// running before the subtotal so discounts, tax and the total all derive from the member price. A
    /// no-op for guests and customers with no pricing role — <see cref="ICustomerRoleService.ResolvePriceAsync"/>
    /// returns the base price — so the common checkout path is unchanged.
    /// </summary>
    private async Task<List<LineWork>> ApplyGroupPricingAsync(List<LineWork> lines, CancellationToken ct)
    {
        if (_current.UserId is not Guid userId || lines.Count == 0)
            return lines;

        var customerId = await _db.Customers
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);
        if (customerId is not Guid cid)
            return lines;

        var roleIds = await _customerRoles.GetCustomerRoleIdsAsync(cid, ct);
        if (roleIds.Count == 0)
            return lines;

        var adjusted = new List<LineWork>(lines.Count);
        foreach (var line in lines)
        {
            var price = await _customerRoles.ResolvePriceAsync(
                line.ProductId, line.ProductVariantId, line.UnitPrice, roleIds, ct);
            adjusted.Add(price < line.UnitPrice ? line with { UnitPrice = price } : line);
        }

        return adjusted;
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
        bool CouponValid,
        bool IsPickup = false);
}
