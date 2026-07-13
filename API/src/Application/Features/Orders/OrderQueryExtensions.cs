using VSky.Domain.Entities;
using VSky.Domain.Enums;

namespace VSky.Application.Features.Orders;

/// <summary>Shared query predicates for orders.</summary>
public static class OrderQueryExtensions
{
    /// <summary>
    /// Excludes <b>provisional</b> orders — ones created only to hold a redirect (Stripe Checkout) payment
    /// session that was never paid (the buyer cancelled or abandoned payment on the gateway). Such orders
    /// carry a <see cref="Order.SourceCartId"/> and are still <see cref="PaymentStatus.Pending"/>; once
    /// payment is captured they no longer match and appear in listings normally. Applied to the admin,
    /// customer and store-queue lists so mere payment attempts don't look like real orders — while the
    /// orders stay loadable by id for the checkout confirm/retry flow (hence no global query filter).
    /// </summary>
    public static IQueryable<Order> ExcludeUnpaidRedirect(this IQueryable<Order> query)
        => query.Where(o => o.SourceCartId == null || o.PaymentStatus != PaymentStatus.Pending);
}
