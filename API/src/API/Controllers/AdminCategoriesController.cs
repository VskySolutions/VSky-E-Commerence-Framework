using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.Categories;

namespace VSky.API.Controllers;

/// <summary>Manage catalog categories (self-referencing tree, SEO metadata, ordering, enable/disable).</summary>
[Route("api/admin/categories")]
[RequireModule(Modules.Catalog)]
public class AdminCategoriesController : ApiControllerBase
{
    /// <summary>List categories (paged), optionally filtered by name.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedList<CategoryDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        => Ok(await Mediator.Send(new ListCategoriesQuery(page, pageSize, search)));

    /// <summary>Get a single category by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Get(Guid id)
        => Ok(await Mediator.Send(new GetCategoryQuery(id)));

    /// <summary>Get the full category tree (root nodes with nested children).</summary>
    [HttpGet("tree")]
    public async Task<ActionResult<List<CategoryTreeNodeDto>>> Tree()
        => Ok(await Mediator.Send(new GetCategoryTreeQuery()));

    /// <summary>Create a new category.</summary>
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryCommand command)
        => Ok(await Mediator.Send(command));

    /// <summary>Update an existing category (route id wins over any id in the body).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, [FromBody] UpdateCategoryCommand command)
        => Ok(await Mediator.Send(command with { Id = id }));

    /// <summary>Delete (soft) a category.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}
