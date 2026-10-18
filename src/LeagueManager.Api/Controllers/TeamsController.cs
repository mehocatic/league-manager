using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Teams;
using Microsoft.AspNetCore.Mvc;

namespace LeagueManager.Api.Controllers;

// Controllers stay thin: bind the request, call one handler, translate the result to HTTP.
[Route("api/teams")]
public sealed class TeamsController(
    IQueryHandler<GetTeamsQuery, IReadOnlyList<TeamDto>> getTeams,
    IQueryHandler<GetTeamByIdQuery, Result<TeamDto>> getTeamById,
    IQueryHandler<GetTeamPlayersQuery, Result<IReadOnlyList<PlayerDto>>> getTeamPlayers,
    ICommandHandler<CreateTeamCommand, Result<TeamDto>> createTeam) : ApiController
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await getTeams.HandleAsync(new GetTeamsQuery(), cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<TeamDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await getTeamById.HandleAsync(new GetTeamByIdQuery(id), cancellationToken);
        return result.Match<IActionResult>(Ok, ErrorResult);
    }

    [HttpGet("{id:int}/players")]
    [ProducesResponseType<IReadOnlyList<PlayerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayers(int id, CancellationToken cancellationToken)
    {
        var result = await getTeamPlayers.HandleAsync(new GetTeamPlayersQuery(id), cancellationToken);
        return result.Match<IActionResult>(Ok, ErrorResult);
    }

    // TODO(JWT milestone): restrict to the admin role.
    [HttpPost]
    [ProducesResponseType<TeamDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        var result = await createTeam.HandleAsync(command, cancellationToken);

        // 201 with a Location header pointing at GET /api/teams/{id}, as REST clients expect after a create.
        return result.Match<IActionResult>(
            team => CreatedAtAction(nameof(GetById), new { id = team.Id }, team),
            ErrorResult);
    }
}
