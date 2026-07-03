using Microsoft.AspNetCore.Mvc;
using VSky.Application.Common.Authorization;
using VSky.API.Authorization;
using VSky.Application.Features.Localization;

namespace VSky.API.Controllers;

/// <summary>Author translations for entity content (REQ-STF-004). Keyed by (entity type, entity id).</summary>
[Route("api/admin/translations")]
[RequireModule(Modules.Languages)]
public class AdminTranslationsController : ApiControllerBase
{
    /// <summary>Get all authored translations for one entity.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ContentTranslationDto>>> Get([FromQuery] string entityType, [FromQuery] Guid entityId)
        => Ok(await Mediator.Send(new GetContentTranslationsQuery(entityType, entityId)));

    /// <summary>Upsert the translations for one entity in one language (blank value removes a field).</summary>
    [HttpPut]
    public async Task<ActionResult<List<ContentTranslationDto>>> Set([FromBody] SetContentTranslationsCommand command)
        => Ok(await Mediator.Send(command));
}
