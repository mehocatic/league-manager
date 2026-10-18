using LeagueManager.Domain.Entities;

namespace LeagueManager.Domain.Standings;

public sealed record StandingRow(
    int Position,
    int TeamId,
    string Team,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int Points)
{
    public int GoalDifference => GoalsFor - GoalsAgainst;
}

// Pure league rules with no database access, so they can be unit tested with plain objects.
public static class StandingsCalculator
{
    public const int PointsForWin = 3;
    public const int PointsForDraw = 1;

    public static IReadOnlyList<StandingRow> Calculate(IEnumerable<Team> teams, IEnumerable<Match> matches)
    {
        // Every team gets a row, so a team that has not played yet still appears with zeros.
        var tallies = teams.ToDictionary(t => t.Id, t => new Tally(t.Id, t.Name));

        foreach (var match in matches)
        {
            // Scheduled matches have no score yet and do not count.
            if (match.HomeGoals is not { } home || match.AwayGoals is not { } away)
            {
                continue;
            }

            tallies[match.HomeTeamId].Record(home, away);
            tallies[match.AwayTeamId].Record(away, home);
        }

        // Tie-breakers as applied in the official 2. liga FBiH table: points, then overall goal difference,
        // then goals scored. Head-to-head is NOT used (e.g. BAJER 99 beat PODGRMEČ but is ranked below it on
        // goal difference). The name is only a final fallback so the order is always deterministic.
        return tallies.Values
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalsFor - t.GoalsAgainst)
            .ThenByDescending(t => t.GoalsFor)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .Select((t, index) => new StandingRow(
                index + 1, t.TeamId, t.Name, t.Won + t.Drawn + t.Lost, t.Won, t.Drawn, t.Lost, t.GoalsFor, t.GoalsAgainst, t.Points))
            .ToList();
    }

    // Mutable accumulator used only during the calculation; callers only ever see the immutable StandingRow.
    private sealed class Tally(int teamId, string name)
    {
        public int TeamId { get; } = teamId;
        public string Name { get; } = name;
        public int Won { get; private set; }
        public int Drawn { get; private set; }
        public int Lost { get; private set; }
        public int GoalsFor { get; private set; }
        public int GoalsAgainst { get; private set; }
        public int Points => Won * PointsForWin + Drawn * PointsForDraw;

        public void Record(int scored, int conceded)
        {
            GoalsFor += scored;
            GoalsAgainst += conceded;

            if (scored > conceded) Won++;
            else if (scored == conceded) Drawn++;
            else Lost++;
        }
    }
}
