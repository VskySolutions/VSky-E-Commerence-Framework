using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Features.Tax;

namespace VSky.API.Controllers;

/// <summary>Manage the singleton tax provider configuration (REQ-TAX-002).</summary>
[Route("api/admin/tax")]
[RequireModule(Modules.Tax)]
public class AdminTaxController : ApiControllerBase
{
    /// <summary>Get the current tax configuration, creating it with defaults if none exists.</summary>
    [HttpGet]
    public async Task<ActionResult<TaxConfigurationDto>> Get()
        => Ok(await Mediator.Send(new GetTaxConfigurationQuery()));

    /// <summary>Create or update the tax configuration (active provider, flat-rate %, enabled, cache TTL).</summary>
    [HttpPut]
    public async Task<ActionResult<TaxConfigurationDto>> Update([FromBody] UpdateTaxConfigurationCommand command)
        => Ok(await Mediator.Send(command));
}
