using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Features.CustomerRoles;

namespace VSky.API.Controllers;

/// <summary>
/// Manage customer roles: group-pricing rules and catalog-access restrictions (REQ-CUS-003). Distinct from
/// the admin RBAC roles.
/// </summary>
[Route("api/admin/customer-roles")]
[RequireModule(Modules.Customers)]
public class AdminCustomerRolesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CustomerRoleDto>>> List()
        => Ok(await Mediator.Send(new ListCustomerRolesQuery()));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerRoleDto>> Get(Guid id)
        => Ok(await Mediator.Send(new GetCustomerRoleQuery(id)));

    [HttpPost]
    public async Task<ActionResult<CustomerRoleDto>> Create([FromBody] CreateCustomerRoleCommand command)
        => Ok(await Mediator.Send(command));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerRoleDto>> Update(Guid id, [FromBody] UpdateCustomerRoleCommand command)
        => Ok(await Mediator.Send(command with { Id = id }));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteCustomerRoleCommand(id));
        return NoContent();
    }

    /// <summary>Replace the explicit group prices of a role.</summary>
    [HttpPut("{id:guid}/group-prices")]
    public async Task<ActionResult<CustomerRoleDto>> SetGroupPrices(Guid id, [FromBody] SetGroupPricesCommand command)
        => Ok(await Mediator.Send(command with { RoleId = id }));

    /// <summary>Restrict a product to a set of roles (empty = visible to all).</summary>
    [HttpPut("products/{productId:guid}/restrictions")]
    public async Task<IActionResult> SetProductRestrictions(Guid productId, [FromBody] SetProductRoleRestrictionsCommand command)
    {
        await Mediator.Send(command with { ProductId = productId });
        return NoContent();
    }

    /// <summary>Restrict a category to a set of roles (empty = visible to all).</summary>
    [HttpPut("categories/{categoryId:guid}/restrictions")]
    public async Task<IActionResult> SetCategoryRestrictions(Guid categoryId, [FromBody] SetCategoryRoleRestrictionsCommand command)
    {
        await Mediator.Send(command with { CategoryId = categoryId });
        return NoContent();
    }
}
