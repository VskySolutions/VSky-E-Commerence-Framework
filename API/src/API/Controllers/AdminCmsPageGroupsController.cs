using Microsoft.AspNetCore.Mvc;
using VSky.API.Authorization;
using VSky.Application.Common.Authorization;
using VSky.Application.Features.CmsPageGroups;

namespace VSky.API.Controllers;

/// <summary>Manage CMS page groups (the organising columns used by the storefront footer/nav).</summary>
[Route("api/admin/cms-page-groups")]
[RequireModule(Modules.Cms)]
public class AdminCmsPageGroupsController : ApiControllerBase
{
    /// <summary>List all page groups ordered by display order.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CmsPageGroupDto>>> List()
        => Ok(await Mediator.Send(new ListCmsPageGroupsQuery()));

    /// <summary>Get a single page group by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CmsPageGroupDto>> Get(Guid id)
        => Ok(await Mediator.Send(new GetCmsPageGroupQuery(id)));

    /// <summary>Create a new page group.</summary>
    [HttpPost]
    public async Task<ActionResult<CmsPageGroupDto>> Create([FromBody] CreateCmsPageGroupCommand command)
        => Ok(await Mediator.Send(command));

    /// <summary>Update an existing page group (route id wins over any id in the body).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CmsPageGroupDto>> Update(Guid id, [FromBody] UpdateCmsPageGroupCommand command)
        => Ok(await Mediator.Send(command with { Id = id }));

    /// <summary>Delete (soft) a page group.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteCmsPageGroupCommand(id));
        return NoContent();
    }
}
