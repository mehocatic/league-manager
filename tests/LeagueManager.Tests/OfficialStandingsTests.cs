using LeagueManager.Application.Standings;
using LeagueManager.Domain.Standings;

namespace LeagueManager.Tests;

public class OfficialStandingsTests
{
    [Fact]
    public async Task Calculated_table_matches_the_official_table_after_round_9()
    {
        await using var app = await TestApp.CreateAsync();

        var standings = await app.QueryAsync<GetStandingsQuery, IReadOnlyList<StandingRow>>(new GetStandingsQuery());

        // Comparing whole CSV lines checks every column (position, played, W/D/L, goals, points) at once,
        // and on failure xUnit shows exactly which row differs from the federation's table.
        var official = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "SeedData", "official-standings-after-round-9.csv"))
            .Skip(1)
            .Where(line => line.Length > 0);
        var calculated = standings.Select(r =>
            $"{r.Position},{r.Team},{r.Played},{r.Won},{r.Drawn},{r.Lost},{r.GoalsFor},{r.GoalsAgainst},{r.Points}");

        Assert.Equal(official, calculated);
    }
}
