using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.BackgroundTasks;

namespace VSky.API.Controllers;

/// <summary>Background task monitoring for the admin dashboard.</summary>
[Route("api/admin/background-tasks")]
[RequireModule(Modules.BackgroundTasks)]
public class AdminBackgroundTasksController : ApiControllerBase
{
    /// <summary>Last execution result and next scheduled run per background task.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BackgroundTaskStatus>>> Get()
        => Ok(await Mediator.Send(new GetBackgroundTasksQuery()));
}
