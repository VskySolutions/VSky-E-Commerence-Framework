using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Features.AdminAlerts;

namespace VSky.API.Controllers;

/// <summary>Surface and resolve operational admin alerts (WO-75 AC-TEN-003.5).</summary>
[Route("api/admin/alerts")]
[RequireModule(Modules.Alerts)]
public class AdminAlertsController : ApiControllerBase
{
    /// <summary>List alerts, optionally restricting to unresolved ones.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAlertDto>>> List([FromQuery] bool? unresolvedOnly)
        => Ok(await Mediator.Send(new ListAdminAlertsQuery(unresolvedOnly)));

    /// <summary>Mark an alert as resolved.</summary>
    [HttpPut("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        await Mediator.Send(new ResolveAdminAlertCommand(id));
        return NoContent();
    }
}
