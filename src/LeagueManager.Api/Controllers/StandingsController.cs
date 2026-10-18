using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Standings;
using LeagueManager.Domain.Standings;
using Microsoft.AspNetCore.Mvc;

namespace LeagueManager.Api.Controllers;

[Route("api")]
public sealed class StandingsController(
    IQueryHandler<GetStandingsQuery, IReadOnlyList<StandingRow>> getStandings,
    IQueryHandler<GetTopScorersQuery, Result<IReadOnlyList<TopScorerDto>>> getTopScorers) : ApiController
{
    [HttpGet("standings")]
    [ProducesResponseType<IReadOnlyList<StandingRow>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStandings(CancellationToken cancellationToken) =>
        Ok(await getStandings.HandleAsync(new GetStandingsQuery(), cancellationToken));

    [HttpGet("top-scorers")]
    [ProducesResponseType<IReadOnlyList<TopScorerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopScorers([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var result = await getTopScorers.HandleAsync(new GetTopScorersQuery(limit), cancellationToken);
        return result.Match<IActionResult>(Ok, ErrorResult);
    }
}
