using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VSky.Application.Features.Recaptcha;

namespace VSky.API.Controllers;

/// <summary>Public storefront configuration endpoints consumed by the client app (WO-106/108).</summary>
[Route("api/storefront/config")]
[AllowAnonymous]
public class StorefrontConfigController : ApiControllerBase
{
    /// <summary>Public reCAPTCHA config (Site Key, version, per-form flags) — never the Secret Key.</summary>
    [HttpGet("recaptcha")]
    public async Task<ActionResult<PublicRecaptchaConfigDto>> Recaptcha()
        => Ok(await Mediator.Send(new GetPublicRecaptchaConfigQuery()));
}
