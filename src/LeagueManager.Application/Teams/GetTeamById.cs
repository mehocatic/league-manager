using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Teams;

public sealed record GetTeamByIdQuery(int Id) : IQuery<Result<TeamDto>>;

internal sealed class GetTeamByIdQueryHandler(ILeagueDbContext db) : IQueryHandler<GetTeamByIdQuery, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> HandleAsync(GetTeamByIdQuery query, CancellationToken cancellationToken)
    {
        var team = await db.Teams
            .Where(t => t.Id == query.Id)
            .Select(t => new TeamDto(t.Id, t.Name, t.City))
            .SingleOrDefaultAsync(cancellationToken);

        return team is null ? Error.NotFound($"Team {query.Id} was not found.") : team;
    }
}
