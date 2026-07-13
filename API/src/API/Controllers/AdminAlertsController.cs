using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.AdminAlerts;

namespace VSky.API.Controllers;

/// <summary>Surface and resolve operational admin alerts (WO-75 AC-TEN-003.5).</summary>
[Route("api/admin/alerts")]
[RequireModule(Modules.Alerts)]
public class AdminAlertsController : ApiControllerBase
{
    /// <summary>Paged alerts (newest first), filterable by search, severity and resolved state.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedList<AdminAlertDto>>> List([FromQuery] ListAdminAlertsQuery query)
        => Ok(await Mediator.Send(query));

    /// <summary>Mark an alert as resolved.</summary>
    [HttpPut("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        await Mediator.Send(new ResolveAdminAlertCommand(id));
        return NoContent();
    }
}
