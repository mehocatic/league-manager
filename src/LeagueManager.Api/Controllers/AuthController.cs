using LeagueManager.Api.Authentication;
using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Auth;
using LeagueManager.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LeagueManager.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController(ICommandHandler<LoginCommand, Result<AccessToken>> login) : ApiController
{
    [HttpPost("login")]
    [EnableRateLimiting(AuthenticationSetup.LoginRateLimitPolicy)]
    [ProducesResponseType<AccessToken>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await login.HandleAsync(command, cancellationToken);
        return result.Match<IActionResult>(Ok, ErrorResult);
    }
}
