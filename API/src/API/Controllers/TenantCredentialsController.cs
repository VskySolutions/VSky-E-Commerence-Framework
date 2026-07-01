using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.Credentials;

namespace VSky.API.Controllers;

/// <summary>Manage encrypted third-party credentials. Values are never returned in plaintext.</summary>
[Route("api/tenant/credentials")]
[RequireModule(Modules.Credentials)]
public class TenantCredentialsController : ApiControllerBase
{
    /// <summary>List all stored credentials (masked).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CredentialSummaryDto>>> List()
        => Ok(await Mediator.Send(new ListCredentialsQuery()));

    /// <summary>Get a single stored credential (masked).</summary>
    [HttpGet("{serviceType}")]
    public async Task<ActionResult<CredentialSummaryDto>> Get(string serviceType)
        => Ok(await Mediator.Send(new GetCredentialQuery(serviceType)));

    /// <summary>Create or replace the credential for a service type.</summary>
    [HttpPut("{serviceType}")]
    public async Task<ActionResult<CredentialSummaryDto>> Upsert(string serviceType, [FromBody] UpsertCredentialRequest body)
        => Ok(await Mediator.Send(new UpsertCredentialCommand(serviceType, body.Value, body.Description)));

    /// <summary>Test connectivity of a stored credential before activation.</summary>
    [HttpPost("{serviceType}/test")]
    public async Task<ActionResult<ConnectivityTestResult>> Test(string serviceType)
        => Ok(await Mediator.Send(new TestCredentialCommand(serviceType)));

    /// <summary>Delete a stored credential.</summary>
    [HttpDelete("{serviceType}")]
    public async Task<IActionResult> Delete(string serviceType)
    {
        await Mediator.Send(new DeleteCredentialCommand(serviceType));
        return NoContent();
    }

    public record UpsertCredentialRequest(string Value, string? Description);
}
