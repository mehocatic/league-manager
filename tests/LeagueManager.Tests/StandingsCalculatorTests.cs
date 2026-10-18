using LeagueManager.Domain.Entities;
using LeagueManager.Domain.Standings;

namespace LeagueManager.Tests;

public class StandingsCalculatorTests
{
    private static List<Team> Teams(params string[] names) =>
        names.Select((name, i) => new Team { Id = i + 1, Name = name }).ToList();

    private static Match Result(int homeTeamId, int awayTeamId, int? homeGoals, int? awayGoals) =>
        new() { HomeTeamId = homeTeamId, AwayTeamId = awayTeamId, HomeGoals = homeGoals, AwayGoals = awayGoals };

    [Fact]
    public void Win_gives_three_points_draw_one_and_scheduled_matches_are_ignored()
    {
        var teams = Teams("Home", "Away", "Idle");
        var matches = new[] { Result(1, 2, 2, 0), Result(2, 1, 1, 1), Result(3, 1, null, null) };

        var table = StandingsCalculator.Calculate(teams, matches);

        var home = table.Single(r => r.Team == "Home");
        Assert.Equal((2, 1, 1, 0, 3, 1, 4), (home.Played, home.Won, home.Drawn, home.Lost, home.GoalsFor, home.GoalsAgainst, home.Points));

        var away = table.Single(r => r.Team == "Away");
        Assert.Equal((2, 0, 1, 1, 1), (away.Played, away.Won, away.Drawn, away.Lost, away.Points));

        // A team without played matches is still listed, with zeros.
        var idle = table.Single(r => r.Team == "Idle");
        Assert.Equal((0, 0), (idle.Played, idle.Points));
    }

    [Fact]
    public void Equal_points_are_decided_by_goal_difference_not_head_to_head()
    {
        // A beats B, but B finishes higher on overall goal difference, as in the official FBiH table.
        var teams = Teams("A", "B", "C", "D");
        var matches = new[] { Result(1, 2, 1, 0), Result(3, 1, 1, 0), Result(2, 4, 4, 0) };

        var table = StandingsCalculator.Calculate(teams, matches);

        Assert.Equal(["B", "C", "A", "D"], table.Select(r => r.Team));
        Assert.Equal([1, 2, 3, 4], table.Select(r => r.Position));
    }

    [Fact]
    public void Equal_points_and_goal_difference_are_decided_by_goals_scored()
    {
        // All four teams have 1 point and goal difference 0; the 2:2 teams rank above the 0:0 teams
        // even though their names sort last, which proves goals scored is used before the name fallback.
        var teams = Teams("A Goalless", "B Goalless", "Y Scoring", "Z Scoring");
        var matches = new[] { Result(1, 2, 0, 0), Result(3, 4, 2, 2) };

        var table = StandingsCalculator.Calculate(teams, matches);

        Assert.Equal(["Y Scoring", "Z Scoring", "A Goalless", "B Goalless"], table.Select(r => r.Team));
    }
}
