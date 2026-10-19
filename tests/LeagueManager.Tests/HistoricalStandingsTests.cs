using LeagueManager.Application.Common;
using LeagueManager.Application.Standings;
using LeagueManager.Domain.Standings;

namespace LeagueManager.Tests;

public class HistoricalStandingsTests
{
    [Fact]
    public async Task Table_and_scorers_up_to_a_round_ignore_later_rounds_and_bypass_the_cache()
    {
        await using var app = await TestApp.CreateAsync();

        var current = await app.QueryAsync<GetStandingsQuery, IReadOnlyList<StandingRow>>(new GetStandingsQuery());
        var afterRound1 = await app.QueryAsync<GetStandingsQuery, IReadOnlyList<StandingRow>>(new GetStandingsQuery(1));

        // All 7 round 1 matches are played, so after round 1 every team has exactly one game.
        Assert.All(afterRound1, row => Assert.Equal(1, row.Played));
        Assert.All(current, row => Assert.Equal(9, row.Played));

        // The historical request did not overwrite the cached current table.
        Assert.Same(current, await app.QueryAsync<GetStandingsQuery, IReadOnlyList<StandingRow>>(new GetStandingsQuery()));

        var scorersAfterRound1 = (await app.QueryAsync<GetTopScorersQuery, Result<IReadOnlyList<TopScorerDto>>>(new GetTopScorersQuery(1, 1))).Unwrap();
        var scorersNow = (await app.QueryAsync<GetTopScorersQuery, Result<IReadOnlyList<TopScorerDto>>>(new GetTopScorersQuery(1))).Unwrap();
        Assert.Equal(("Almir Marić", 3), (scorersAfterRound1[0].Player, scorersAfterRound1[0].Goals));
        Assert.Equal(("Almir Marić", 6), (scorersNow[0].Player, scorersNow[0].Goals));
    }
}
