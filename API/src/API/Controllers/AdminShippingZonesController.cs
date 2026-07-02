using Microsoft.AspNetCore.Mvc;
using VSky.API.Authorization;
using VSky.Application.Common.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.ShippingZones;

namespace VSky.API.Controllers;

/// <summary>Manage geographic shipping zones used by custom shipping-method rates (WO-43).</summary>
[Route("api/admin/shipping-zones")]
[RequireModule(Modules.Shipping)]
public class AdminShippingZonesController : ApiControllerBase
{
    /// <summary>List shipping zones (paged), optionally filtered by name.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedList<ShippingZoneDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        => Ok(await Mediator.Send(new ListShippingZonesQuery(page, pageSize, search)));

    /// <summary>Get a single shipping zone by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShippingZoneDto>> Get(Guid id)
        => Ok(await Mediator.Send(new GetShippingZoneQuery(id)));

    /// <summary>Create a new shipping zone.</summary>
    [HttpPost]
    public async Task<ActionResult<ShippingZoneDto>> Create([FromBody] CreateShippingZoneCommand command)
        => Ok(await Mediator.Send(command));

    /// <summary>Update an existing shipping zone (route id wins over any id in the body).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShippingZoneDto>> Update(Guid id, [FromBody] UpdateShippingZoneCommand command)
        => Ok(await Mediator.Send(command with { Id = id }));

    /// <summary>Delete (soft) a shipping zone.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteShippingZoneCommand(id));
        return NoContent();
    }
}
