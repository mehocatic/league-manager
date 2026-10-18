using LeagueManager.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace LeagueManager.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    // The single place where business errors become HTTP status codes, returned as RFC 9457 ProblemDetails
    // so every error response has the same JSON shape for the frontend.
    protected IActionResult ErrorResult(Error error) =>
        Problem(
            statusCode: error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.ExternalService => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError
            },
            detail: error.Message);
}
