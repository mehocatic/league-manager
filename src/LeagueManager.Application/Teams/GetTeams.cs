using LeagueManager.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Teams;

public sealed record GetTeamsQuery : IQuery<IReadOnlyList<TeamDto>>;

internal sealed class GetTeamsQueryHandler(ILeagueDbContext db) : IQueryHandler<GetTeamsQuery, IReadOnlyList<TeamDto>>
{
    public async Task<IReadOnlyList<TeamDto>> HandleAsync(GetTeamsQuery query, CancellationToken cancellationToken) =>
        // Projecting in the query makes SQL select only these three columns, and nothing gets change-tracked.
        await db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamDto(t.Id, t.Name, t.City))
            .ToListAsync(cancellationToken);
}
