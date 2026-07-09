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
    /// <summary>List customers (paged), optionally filtered by name/email, email-verified, active and/or tax-exempt state.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedList<CustomerListItemDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] bool? emailVerified = null, [FromQuery] bool? isActive = null, [FromQuery] bool? isTaxExempt = null)
        => Ok(await Mediator.Send(new ListCustomersQuery(page, pageSize, search, emailVerified, isActive, isTaxExempt)));

    /// <summary>Get a single customer's full detail: profile, tax-exemption, roles, addresses and order history.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> Get(Guid id)
        => Ok(await Mediator.Send(new GetCustomerDetailQuery(id)));

    /// <summary>Get a customer's tax-exemption configuration.</summary>
    [HttpGet("{id:guid}/tax-exemption")]
    public async Task<ActionResult<CustomerTaxExemptionDto>> GetTaxExemption(Guid id)
        => Ok(await Mediator.Send(new GetCustomerTaxExemptionQuery(id)));

    /// <summary>Set (or clear) a customer's tax-exemption flag and supporting certificate / VAT id.</summary>
    [HttpPut("{id:guid}/tax-exemption")]
    public async Task<ActionResult<CustomerTaxExemptionDto>> SetTaxExemption(Guid id, [FromBody] SetCustomerTaxExemptionCommand command)
        => Ok(await Mediator.Send(command with { CustomerId = id }));

    /// <summary>Get the customer's store-credit balance and ledger (WO-48).</summary>
    [HttpGet("{id:guid}/store-credit")]
    public async Task<ActionResult<StoreCreditDto>> GetStoreCredit(Guid id)
        => Ok(await Mediator.Send(new GetCustomerStoreCreditQuery(id)));

    /// <summary>Manually grant store credit to the customer (WO-48).</summary>
    [HttpPost("{id:guid}/store-credit")]
    public async Task<ActionResult<StoreCreditDto>> IssueStoreCredit(Guid id, [FromBody] IssueStoreCreditCommand command)
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
