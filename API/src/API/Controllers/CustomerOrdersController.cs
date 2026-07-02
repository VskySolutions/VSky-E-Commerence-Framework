using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Models;
using VSky.Application.Features.Orders;

namespace VSky.API.Controllers;

/// <summary>The authenticated customer's own order history and order detail (WO-45 buyer view).</summary>
[Route("api/customer/orders")]
[Authorize]
public class CustomerOrdersController : ApiControllerBase
{
    /// <summary>List the current customer's own orders (paged), newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedList<OrderSummaryDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => Ok(await Mediator.Send(new ListMyOrdersQuery(page, pageSize)));

    /// <summary>Get one of the current customer's own orders including its line items.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id)
        => Ok(await Mediator.Send(new GetMyOrderQuery(id)));
}
