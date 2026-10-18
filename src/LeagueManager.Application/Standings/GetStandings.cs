using LeagueManager.Application.Abstractions;
using LeagueManager.Domain.Entities;
using LeagueManager.Domain.Standings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LeagueManager.Application.Standings;

// StandingRow is returned as-is: it is an immutable calculated value, not an entity, so it needs no separate DTO.
public sealed record GetStandingsQuery : IQuery<IReadOnlyList<StandingRow>>;

internal sealed class GetStandingsQueryHandler(ILeagueDbContext db, IMemoryCache cache, ILogger<GetStandingsQueryHandler> logger)
    : IQueryHandler<GetStandingsQuery, IReadOnlyList<StandingRow>>
{
    public async Task<IReadOnlyList<StandingRow>> HandleAsync(GetStandingsQuery query, CancellationToken cancellationToken)
    {
        var standings = await cache.GetOrCreateAsync(StandingsCache.StandingsKey, entry =>
        {
            entry.SetOptions(StandingsCache.EntryOptions);
            return CalculateAsync(cancellationToken);
        });

        return standings!;
    }

    private async Task<IReadOnlyList<StandingRow>> CalculateAsync(CancellationToken cancellationToken)
    {
        // Projecting into new entity instances loads only the columns the calculator needs, without change tracking.
        var teams = await db.Teams
            .Select(t => new Team { Id = t.Id, Name = t.Name })
            .ToListAsync(cancellationToken);
        var playedMatches = await db.Matches
            .Where(m => m.HomeGoals != null && m.AwayGoals != null)
            .Select(m => new Match { HomeTeamId = m.HomeTeamId, AwayTeamId = m.AwayTeamId, HomeGoals = m.HomeGoals, AwayGoals = m.AwayGoals })
            .ToListAsync(cancellationToken);

        var standings = StandingsCalculator.Calculate(teams, playedMatches);

        // Logged so the demo can show that only the first request after a change hits the database.
        logger.LogInformation("Standings cache miss: recalculated from {Matches} played matches", playedMatches.Count);
        return standings;
    }
}
