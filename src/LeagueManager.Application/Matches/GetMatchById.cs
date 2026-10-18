using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Matches;

public sealed record GetMatchByIdQuery(int Id) : IQuery<Result<MatchDetailsDto>>;

internal sealed class GetMatchByIdQueryHandler(ILeagueDbContext db) : IQueryHandler<GetMatchByIdQuery, Result<MatchDetailsDto>>
{
    public async Task<Result<MatchDetailsDto>> HandleAsync(GetMatchByIdQuery query, CancellationToken cancellationToken)
    {
        var match = await db.Matches
            .Where(m => m.Id == query.Id)
            .SelectDetails()
            .SingleOrDefaultAsync(cancellationToken);

        return match is null ? Error.NotFound($"Match {query.Id} was not found.") : match;
    }
}
