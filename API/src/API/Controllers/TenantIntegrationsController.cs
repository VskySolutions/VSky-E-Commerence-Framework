using Microsoft.AspNetCore.Mvc;
using VSky.API.Authorization;
using VSky.Application.Common.Authorization;
using VSky.Application.Common.Models;
using VSky.Application.Features.Integrations;

namespace VSky.API.Controllers;

/// <summary>
/// Dynamic Credential Vault admin API (WO-7): manage integration providers, their credential field
/// definitions, and stored values. Secret values are encrypted at rest and never returned in plaintext.
/// The auto-generated admin UI is built from the provider "form" endpoint.
/// </summary>
[Route("api/tenant/integrations")]
[RequireModule(Modules.Credentials)]
public class TenantIntegrationsController : ApiControllerBase
{
    /// <summary>List providers grouped by category, with optional search/category/status filters.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IntegrationCategoryDto>>> List(
        [FromQuery] string? search, [FromQuery] string? category, [FromQuery] bool? enabled)
        => Ok(await Mediator.Send(new ListIntegrationsQuery(search, category, enabled)));

    // --- Providers ----------------------------------------------------------

    /// <summary>Create a provider.</summary>
    [HttpPost("providers")]
    public async Task<ActionResult<IntegrationProviderSummaryDto>> CreateProvider([FromBody] CreateIntegrationProviderCommand command)
        => Ok(await Mediator.Send(command));

    /// <summary>Update a provider (code is immutable).</summary>
    [HttpPut("providers/{id:guid}")]
    public async Task<ActionResult<IntegrationProviderSummaryDto>> UpdateProvider(Guid id, [FromBody] UpdateIntegrationProviderCommand command)
        => Ok(await Mediator.Send(command with { Id = id }));

    /// <summary>Retire a provider (soft delete).</summary>
    [HttpDelete("providers/{id:guid}")]
    public async Task<IActionResult> DeleteProvider(Guid id)
    {
        await Mediator.Send(new DeleteIntegrationProviderCommand(id));
        return NoContent();
    }

    // --- Credential form + values ------------------------------------------

    /// <summary>Get a provider's auto-generated credential form (definitions + masked current values).</summary>
    [HttpGet("providers/{id:guid}/form")]
    public async Task<ActionResult<ProviderFormDto>> GetForm(Guid id)
        => Ok(await Mediator.Send(new GetIntegrationProviderFormQuery(id)));

    /// <summary>Save a provider's credential values (secret fields encrypted; a blank value clears a field).</summary>
    [HttpPut("providers/{id:guid}/credentials")]
    public async Task<ActionResult<ProviderFormDto>> SaveCredentials(Guid id, [FromBody] Dictionary<string, string?> values)
        => Ok(await Mediator.Send(new SaveIntegrationCredentialsCommand(id, values ?? new())));

    /// <summary>Clear a single stored credential field.</summary>
    [HttpDelete("providers/{id:guid}/credentials/{fieldCode}")]
    public async Task<IActionResult> DeleteCredential(Guid id, string fieldCode)
    {
        await Mediator.Send(new DeleteIntegrationCredentialCommand(id, fieldCode));
        return NoContent();
    }

    /// <summary>Probe connectivity using the provider's stored credentials.</summary>
    [HttpPost("providers/{id:guid}/test")]
    public async Task<ActionResult<ConnectivityTestResult>> Test(Guid id)
        => Ok(await Mediator.Send(new TestIntegrationProviderCommand(id)));

    // --- Credential definitions --------------------------------------------

    /// <summary>Add a credential field definition to a provider.</summary>
    [HttpPost("providers/{id:guid}/definitions")]
    public async Task<ActionResult<CredentialDefinitionDto>> CreateDefinition(Guid id, [FromBody] CreateCredentialDefinitionCommand command)
        => Ok(await Mediator.Send(command with { ProviderId = id }));

    /// <summary>Update a credential field definition (field code is immutable).</summary>
    [HttpPut("definitions/{id:guid}")]
    public async Task<ActionResult<CredentialDefinitionDto>> UpdateDefinition(Guid id, [FromBody] UpdateCredentialDefinitionCommand command)
        => Ok(await Mediator.Send(command with { Id = id }));

    /// <summary>Delete a credential field definition and any stored values for it.</summary>
    [HttpDelete("definitions/{id:guid}")]
    public async Task<IActionResult> DeleteDefinition(Guid id)
    {
        await Mediator.Send(new DeleteCredentialDefinitionCommand(id));
        return NoContent();
    }
}
