using Microsoft.AspNetCore.Mvc;
using VSky.API.Authorization;
using VSky.Application.Common.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.OrderRouting;

namespace VSky.API.Controllers;

/// <summary>
/// Order routing administration (WO-51). Exposes a routing preview that runs the engine for a given
/// delivery address + line items, returning the selected store and full evaluation chain.
/// </summary>
/// <remarks>
/// Manual reroute (<c>PUT /admin/orders/{id}/reroute</c>) and the Store Manager order queue (WO-52)
/// operate on persisted orders and are deferred until the Order Management feature introduces the
/// <c>Order</c> entity; the routing algorithm itself is complete and exercised via the preview endpoint.
/// </remarks>
[Route("api/admin/routing")]
[RequireModule(Modules.Stores)]
public class AdminRoutingController : ApiControllerBase
{
    /// <summary>Evaluate routing for a prospective order (stock, delivery zone, proximity) without persisting anything.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<RoutingResult>> Preview([FromBody] RoutingRequest request)
        => Ok(await Mediator.Send(new EvaluateRoutingQuery(request)));
}
