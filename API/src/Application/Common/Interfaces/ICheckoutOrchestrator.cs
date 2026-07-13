using VSky.Application.Common.Models;

namespace VSky.Application.Common.Interfaces;

/// <summary>
/// The checkout capstone (REQ-CHK-003). Orchestrates the already-built commerce services into a single
/// flow: it loads the cart, routes the order to a fulfilment store, enforces the store's guest-ordering
/// policy, quotes shipping, evaluates discounts/coupons, calculates tax, and — on placement — creates
/// the order, authorizes payment, and (on success) finalizes the cart, redeems the coupon, and raises
/// the order-placed events + confirmation email.
/// </summary>
public interface ICheckoutOrchestrator
{
    /// <summary>Prices a cart for the given delivery address without placing an order (read-only preview).</summary>
    Task<CheckoutQuote> QuoteAsync(CheckoutQuoteRequest req, CancellationToken ct);

    /// <summary>Places the order: prices the cart, creates the order, and authorizes payment.</summary>
    Task<CheckoutResult> PlaceAsync(PlaceCheckoutRequest req, CancellationToken ct);

    /// <summary>
    /// Confirms a redirect payment on the buyer's return: verifies the gateway session and, when paid,
    /// captures the payment, consumes the cart, and finalizes the order (emails/events). Idempotent.
    /// </summary>
    Task<CheckoutResult> ConfirmAsync(Guid orderId, CancellationToken ct);

    /// <summary>Re-opens a payment session for a still-pending order (retry after a cancelled redirect payment).</summary>
    Task<CheckoutResult> RetryPaymentAsync(Guid orderId, CancellationToken ct);
}
