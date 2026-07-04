using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.Customers;
using VSky.Application.Features.CustomerRoles;

namespace VSky.API.Controllers;

/// <summary>Admin customer management: list, tax-exemption configuration (REQ-TAX-003) and role assignment (REQ-CUS-003).</summary>
[Route("api/admin/customers")]
[RequireModule(Modules.Customers)]
public class AdminCustomersController : ApiControllerBase
{
    /// <summary>List customers (paged), optionally filtered by name or email.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedList<CustomerListItemDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        => Ok(await Mediator.Send(new ListCustomersQuery(page, pageSize, search)));

    /// <summary>Get a customer's tax-exemption configuration.</summary>
    [HttpGet("{id:guid}/tax-exemption")]
    public async Task<ActionResult<CustomerTaxExemptionDto>> GetTaxExemption(Guid id)
        => Ok(await Mediator.Send(new GetCustomerTaxExemptionQuery(id)));

    /// <summary>Set (or clear) a customer's tax-exemption flag and supporting certificate / VAT id.</summary>
    [HttpPut("{id:guid}/tax-exemption")]
    public async Task<ActionResult<CustomerTaxExemptionDto>> SetTaxExemption(Guid id, [FromBody] SetCustomerTaxExemptionCommand command)
        => Ok(await Mediator.Send(command with { CustomerId = id }));

    /// <summary>Get the customer's assigned customer roles.</summary>
    [HttpGet("{id:guid}/roles")]
    public async Task<ActionResult<List<CustomerRoleDto>>> GetRoles(Guid id)
        => Ok(await Mediator.Send(new GetCustomerRolesQuery(id)));

    /// <summary>Replace the customer's assigned customer roles.</summary>
    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<List<CustomerRoleDto>>> SetRoles(Guid id, [FromBody] AssignCustomerRolesCommand command)
        => Ok(await Mediator.Send(command with { CustomerId = id }));
}
