using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Teams;

public sealed record PlayerDto(int Id, int ShirtNumber, string FirstName, string LastName);

// The admin needs the squads of both teams to pick scorers when entering a result.
public sealed record GetTeamPlayersQuery(int TeamId) : IQuery<Result<IReadOnlyList<PlayerDto>>>;

internal sealed class GetTeamPlayersQueryHandler(ILeagueDbContext db) : IQueryHandler<GetTeamPlayersQuery, Result<IReadOnlyList<PlayerDto>>>
{
    public async Task<Result<IReadOnlyList<PlayerDto>>> HandleAsync(GetTeamPlayersQuery query, CancellationToken cancellationToken)
    {
        // Without this check an unknown team would return an empty list, hiding a wrong id behind a 200.
        if (!await db.Teams.AnyAsync(t => t.Id == query.TeamId, cancellationToken))
        {
            return Error.NotFound($"Team {query.TeamId} was not found.");
        }

        return await db.Players
            .Where(p => p.TeamId == query.TeamId)
            .OrderBy(p => p.ShirtNumber)
            .Select(p => new PlayerDto(p.Id, p.ShirtNumber, p.FirstName, p.LastName))
            .ToListAsync(cancellationToken);
    }
}
