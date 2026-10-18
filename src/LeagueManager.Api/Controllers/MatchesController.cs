using LeagueManager.Api.Contracts;
using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Matches;
using LeagueManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeagueManager.Api.Controllers;

[Route("api/matches")]
public sealed class MatchesController(
    IQueryHandler<GetMatchesQuery, Result<IReadOnlyList<MatchDto>>> getMatches,
    IQueryHandler<GetMatchByIdQuery, Result<MatchDetailsDto>> getMatchById,
    ICommandHandler<EnterResultCommand, Result<MatchDetailsDto>> enterResult,
    ICommandHandler<GenerateMatchSummaryCommand, Result<MatchDetailsDto>> generateSummary) : ApiController
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MatchDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int? round, CancellationToken cancellationToken)
    {
        var result = await getMatches.HandleAsync(new GetMatchesQuery(round), cancellationToken);
        return result.Match<IActionResult>(Ok, ErrorResult);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<MatchDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await getMatchById.HandleAsync(new GetMatchByIdQuery(id), cancellationToken);
        return result.Match<IActionResult>(Ok, ErrorResult);
    }

    // PUT, not POST: the result is a sub-resource of the match that is fully replaced, so repeating the request is safe.
    [HttpPut("{id:int}/result")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<MatchDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnterResult(int id, EnterResultRequest request, CancellationToken cancellationToken)
    {
        var command = new EnterResultCommand(id, request.HomeGoals!.Value, request.AwayGoals!.Value, request.Goals ?? []);
        var result = await enterResult.HandleAsync(command, cancellationToken);
        return result.Match<IActionResult>(Ok, ErrorResult);
    }

    [HttpPost("{id:int}/summary")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<MatchDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GenerateSummary(int id, CancellationToken cancellationToken)
    {
        var result = await generateSummary.HandleAsync(new GenerateMatchSummaryCommand(id), cancellationToken);
        return result.Match<IActionResult>(Ok, ErrorResult);
    }
}
