using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Features.Customers;
using VSky.Application.Features.CustomerRoles;

namespace VSky.API.Controllers;

/// <summary>Admin customer management: tax-exemption configuration (REQ-TAX-003) and role assignment (REQ-CUS-003).</summary>
[Route("api/admin/customers")]
[RequireModule(Modules.Customers)]
public class AdminCustomersController : ApiControllerBase
{
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
