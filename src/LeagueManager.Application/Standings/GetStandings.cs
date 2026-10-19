using LeagueManager.Application.Abstractions;
using LeagueManager.Domain.Entities;
using LeagueManager.Domain.Standings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LeagueManager.Application.Standings;

// StandingRow is returned as-is: it is an immutable calculated value, not an entity, so it needs no separate DTO.
// UpToRound gives the table as it stood after that round (for older round reports). Only the current table is cached:
// it is what every page view asks for, while a historical table is requested rarely.
public sealed record GetStandingsQuery(int? UpToRound = null) : IQuery<IReadOnlyList<StandingRow>>;

internal sealed class GetStandingsQueryHandler(ILeagueDbContext db, IMemoryCache cache, ILogger<GetStandingsQueryHandler> logger)
    : IQueryHandler<GetStandingsQuery, IReadOnlyList<StandingRow>>
{
    public async Task<IReadOnlyList<StandingRow>> HandleAsync(GetStandingsQuery query, CancellationToken cancellationToken)
    {
        if (query.UpToRound is not null)
        {
            return await CalculateAsync(query.UpToRound, cancellationToken);
        }

        var standings = await cache.GetOrCreateAsync(StandingsCache.StandingsKey, entry =>
        {
            entry.SetOptions(StandingsCache.EntryOptions);
            return CalculateAsync(null, cancellationToken);
        });

        return standings!;
    }

    private async Task<IReadOnlyList<StandingRow>> CalculateAsync(int? upToRound, CancellationToken cancellationToken)
    {
        // Projecting into new entity instances loads only the columns the calculator needs, without change tracking.
        var teams = await db.Teams
            .Select(t => new Team { Id = t.Id, Name = t.Name })
            .ToListAsync(cancellationToken);
        var playedMatches = await db.Matches
            .Where(m => m.HomeGoals != null && m.AwayGoals != null)
            .Where(m => upToRound == null || m.Round <= upToRound)
            .Select(m => new Match { HomeTeamId = m.HomeTeamId, AwayTeamId = m.AwayTeamId, HomeGoals = m.HomeGoals, AwayGoals = m.AwayGoals })
            .ToListAsync(cancellationToken);

        var standings = StandingsCalculator.Calculate(teams, playedMatches);

        // Logged so the demo can show that only the first request after a change hits the database.
        logger.LogInformation("Standings cache miss: recalculated from {Matches} played matches", playedMatches.Count);
        return standings;
    }
}
