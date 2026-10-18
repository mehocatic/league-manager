using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Matches;

// Round is optional: the public fixtures page can load one round or the whole season.
public sealed record GetMatchesQuery(int? Round) : IQuery<Result<IReadOnlyList<MatchDto>>>;

internal sealed class GetMatchesQueryHandler(ILeagueDbContext db) : IQueryHandler<GetMatchesQuery, Result<IReadOnlyList<MatchDto>>>
{
    public async Task<Result<IReadOnlyList<MatchDto>>> HandleAsync(GetMatchesQuery query, CancellationToken cancellationToken)
    {
        if (query.Round is < 1)
        {
            return Error.Validation("Round must be 1 or greater.");
        }

        var matches = db.Matches.AsQueryable();
        if (query.Round is { } round)
        {
            matches = matches.Where(m => m.Round == round);
        }

        // A round with no fixtures yet is an empty filter result (200 with []), not a missing resource (404).
        return await matches
            .OrderBy(m => m.Round).ThenBy(m => m.KickOff).ThenBy(m => m.Id)
            .SelectDto()
            .ToListAsync(cancellationToken);
    }
}
