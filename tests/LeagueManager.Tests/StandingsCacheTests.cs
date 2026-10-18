using LeagueManager.Application.Common;
using LeagueManager.Application.Matches;
using LeagueManager.Application.Standings;
using LeagueManager.Domain.Standings;

namespace LeagueManager.Tests;

public class StandingsCacheTests
{
    [Fact]
    public async Task Standings_and_top_scorers_are_cached_until_a_result_is_entered()
    {
        await using var app = await TestApp.CreateAsync();

        var first = await app.QueryAsync<GetStandingsQuery, IReadOnlyList<StandingRow>>(new GetStandingsQuery());
        var second = await app.QueryAsync<GetStandingsQuery, IReadOnlyList<StandingRow>>(new GetStandingsQuery());

        // The same instance means the second call never touched the database.
        Assert.Same(first, second);

        // Let the current top scorer score once more in a round 10 match of his team.
        var topScorer = (await app.QueryAsync<GetTopScorersQuery, Result<IReadOnlyList<TopScorerDto>>>(new GetTopScorersQuery(1))).Unwrap()[0];
        var round10 = (await app.QueryAsync<GetMatchesQuery, Result<IReadOnlyList<MatchDto>>>(new GetMatchesQuery(10))).Unwrap();
        var fixture = round10.Single(m => m.HomeTeamId == topScorer.TeamId || m.AwayTeamId == topScorer.TeamId);
        var scorerIsHome = fixture.HomeTeamId == topScorer.TeamId;

        (await app.CommandAsync<EnterResultCommand, Result<MatchDetailsDto>>(new EnterResultCommand(
            fixture.Id, scorerIsHome ? 1 : 0, scorerIsHome ? 0 : 1, [new GoalInput(topScorer.PlayerId, 30)]))).Unwrap();

        var after = await app.QueryAsync<GetStandingsQuery, IReadOnlyList<StandingRow>>(new GetStandingsQuery());
        Assert.NotSame(first, after);

        var teamBefore = first.Single(r => r.TeamId == topScorer.TeamId);
        var teamAfter = after.Single(r => r.TeamId == topScorer.TeamId);
        Assert.Equal(teamBefore.Played + 1, teamAfter.Played);
        Assert.Equal(teamBefore.Points + StandingsCalculator.PointsForWin, teamAfter.Points);

        var topScorerAfter = (await app.QueryAsync<GetTopScorersQuery, Result<IReadOnlyList<TopScorerDto>>>(new GetTopScorersQuery(1))).Unwrap()[0];
        Assert.Equal((topScorer.PlayerId, topScorer.Goals + 1), (topScorerAfter.PlayerId, topScorerAfter.Goals));
    }
}
