using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LeagueManager.Application.Standings;

public sealed record TopScorerDto(int Rank, int PlayerId, string Player, int TeamId, string Team, int Goals);

// UpToRound works like in GetStandingsQuery: historical lists are calculated, only the current one is cached.
public sealed record GetTopScorersQuery(int Limit, int? UpToRound = null) : IQuery<Result<IReadOnlyList<TopScorerDto>>>;

internal sealed class GetTopScorersQueryHandler(ILeagueDbContext db, IMemoryCache cache)
    : IQueryHandler<GetTopScorersQuery, Result<IReadOnlyList<TopScorerDto>>>
{
    public const int MaxLimit = 100;

    public async Task<Result<IReadOnlyList<TopScorerDto>>> HandleAsync(GetTopScorersQuery query, CancellationToken cancellationToken)
    {
        if (query.Limit is < 1 or > MaxLimit)
        {
            return Error.Validation($"Limit must be between 1 and {MaxLimit}.");
        }

        if (query.UpToRound is not null)
        {
            return (await LoadRankedScorersAsync(query.UpToRound, cancellationToken)).Take(query.Limit).ToList();
        }

        // The full ranked list is cached under one key and trimmed per request; caching per limit would need
        // one key per value and make invalidation guess which keys exist.
        var all = await cache.GetOrCreateAsync(StandingsCache.TopScorersKey, entry =>
        {
            entry.SetOptions(StandingsCache.EntryOptions);
            return LoadRankedScorersAsync(null, cancellationToken);
        });

        return all!.Take(query.Limit).ToList();
    }

    private async Task<IReadOnlyList<TopScorerDto>> LoadRankedScorersAsync(int? upToRound, CancellationToken cancellationToken)
    {
        // GROUP BY runs in SQL, so only one row per scorer comes back instead of every goal.
        var scorers = await db.Goals
            .Where(g => upToRound == null || g.Match.Round <= upToRound)
            .GroupBy(g => new { g.PlayerId, g.Player.FirstName, g.Player.LastName, g.Player.TeamId, Team = g.Player.Team.Name })
            .Select(group => new { group.Key, Goals = group.Count() })
            .OrderByDescending(s => s.Goals).ThenBy(s => s.Key.LastName).ThenBy(s => s.Key.FirstName)
            .ToListAsync(cancellationToken);

        // Shared ranks like a real scorer list (1, 2, 2, 4): a player's rank is 1 + the number of players with more goals.
        var ranked = new List<TopScorerDto>(scorers.Count);
        for (var i = 0; i < scorers.Count; i++)
        {
            var rank = i > 0 && scorers[i].Goals == scorers[i - 1].Goals ? ranked[i - 1].Rank : i + 1;
            var s = scorers[i].Key;
            ranked.Add(new TopScorerDto(rank, s.PlayerId, $"{s.FirstName} {s.LastName}", s.TeamId, s.Team, scorers[i].Goals));
        }

        return ranked;
    }
}
