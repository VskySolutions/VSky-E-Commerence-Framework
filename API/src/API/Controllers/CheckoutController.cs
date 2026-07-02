using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Models;
using VSky.Application.Features.Checkout;

namespace VSky.API.Controllers;

/// <summary>
/// One-page checkout surface (REQ-CHK-003). Public by design — guests check out too, gated per store by
/// the guest-ordering policy (a placement against a store that forbids guest ordering returns 401 for an
/// unauthenticated buyer). <c>quote</c> is a read-only price preview; <c>place</c> finalizes the order and
/// authorizes payment. Shares the <c>api/checkout</c> route prefix with the shipping-rates endpoint via
/// distinct action routes.
/// </summary>
[Route("api/checkout")]
[AllowAnonymous]
public class CheckoutController : ApiControllerBase
{
    /// <summary>Price the caller's cart for a delivery address (subtotal, discounts, shipping, tax, total).</summary>
    [HttpPost("quote")]
    public async Task<ActionResult<CheckoutQuote>> Quote(
        [FromBody] CheckoutQuoteRequest request, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new QuoteCheckoutQuery(request), cancellationToken));

    /// <summary>Place the order: creates the order, authorizes payment, and (on success) confirms checkout.</summary>
    [HttpPost("place")]
    public async Task<ActionResult<CheckoutResult>> Place(
        [FromBody] PlaceCheckoutRequest request, CancellationToken cancellationToken)
        => Ok(await Mediator.Send(new PlaceCheckoutCommand(request), cancellationToken));
}
