using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.Orders;

namespace VSky.API.Controllers;

/// <summary>Admin oversight of orders: list, inspect and manually reroute (WO-52).</summary>
[Route("api/admin/orders")]
[RequireModule(Modules.Stores)]
public class AdminOrdersController : ApiControllerBase
{
    /// <summary>List all orders (paged), newest first, optionally filtered by status.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedList<OrderSummaryDto>>> List(
        [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await Mediator.Send(new ListOrdersQuery(status, page, pageSize)));

    /// <summary>Get a single order including its line items.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id)
        => Ok(await Mediator.Send(new GetOrderQuery(id)));

    /// <summary>Manually reroute an order (route id wins over any id in the body).</summary>
    [HttpPut("{id:guid}/reroute")]
    public async Task<ActionResult<OrderDto>> Reroute(Guid id, [FromBody] RerouteOrderCommand command)
        => Ok(await Mediator.Send(command with { OrderId = id }));
}
