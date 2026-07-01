using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VSky.Application.Features.Authentication;

namespace VSky.API.Controllers;

/// <summary>Authentication endpoints: login, refresh, and logout.</summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    /// <summary>Authenticate and receive an access + refresh token pair.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginCommand command)
        => Ok(await Mediator.Send(command));

    /// <summary>Exchange a valid refresh token for a new access token (rotates the refresh token).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenCommand command)
        => Ok(await Mediator.Send(command));

    /// <summary>Invalidate all active refresh tokens for the current session.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LogoutCommand? command)
    {
        await Mediator.Send(command ?? new LogoutCommand(null));
        return NoContent();
    }
}
