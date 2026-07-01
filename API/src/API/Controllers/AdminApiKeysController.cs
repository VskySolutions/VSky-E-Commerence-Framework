using Microsoft.AspNetCore.Mvc;
using VSky.API.Authorization;
using VSky.Application.Common.Authorization;
using VSky.Application.Features.ApiKeys;

namespace VSky.API.Controllers;

/// <summary>Manage API keys for machine-to-machine callers. The plaintext key is returned only at creation.</summary>
[Route("api/admin/api-keys")]
[RequireModule(Modules.ApiKeys)]
public class AdminApiKeysController : ApiControllerBase
{
    /// <summary>List API keys (masked); the plaintext is never returned here.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> List()
        => Ok(await Mediator.Send(new ListApiKeysQuery()));

    /// <summary>Create a new API key. The response carries the plaintext key exactly once.</summary>
    [HttpPost]
    public async Task<ActionResult<CreatedApiKeyDto>> Create([FromBody] CreateApiKeyCommand command)
        => Ok(await Mediator.Send(command));

    /// <summary>Revoke (deactivate) an API key.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Revoke(Guid id)
    {
        await Mediator.Send(new RevokeApiKeyCommand(id));
        return NoContent();
    }
}
