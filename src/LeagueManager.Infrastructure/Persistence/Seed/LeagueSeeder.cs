using System.Globalization;
using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeagueManager.Infrastructure.Persistence.Seed;

public static class LeagueSeeder
{
    // A fixed seed makes the fictional players and scorers identical on every machine,
    // so the demo path and README screenshots stay reproducible.
    private const int RandomSeed = 2026;
    private const int SquadSize = 18;

    public static async Task SeedAsync(LeagueDbContext db, string dataDirectory, ILogger logger, CancellationToken cancellationToken)
    {
        // Only an empty database is seeded, so a restart never duplicates or overwrites results entered by the admin.
        if (await db.Teams.AnyAsync(cancellationToken))
        {
            return;
        }

        var random = new Random(RandomSeed);
        var teams = ReadTeams(Path.Combine(dataDirectory, "teams.csv"), random);
        var matches = ReadMatches(Path.Combine(dataDirectory, "fixtures.csv"), teams, random);

        // Navigation properties link the whole graph, so one SaveChanges inserts it in a single transaction:
        // either everything is seeded or nothing is.
        db.Teams.AddRange(teams.Values);
        db.Matches.AddRange(matches);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded {Teams} teams, {Players} players, {Matches} matches ({Played} played) and {Goals} goals from {Directory}",
            teams.Count, teams.Values.Sum(t => t.Players.Count), matches.Count,
            matches.Count(m => m.IsPlayed), matches.Sum(m => m.Goals.Count), dataDirectory);
    }

    private static Dictionary<string, Team> ReadTeams(string path, Random random)
    {
        var teams = new Dictionary<string, Team>(StringComparer.Ordinal);

        foreach (var (line, fields) in ReadCsv(path, expectedColumns: 2))
        {
            var team = new Team { Name = fields[0], City = fields[1], Players = FictionalPlayers.CreateSquad(random, SquadSize) };
            if (!teams.TryAdd(team.Name, team))
            {
                throw new InvalidDataException($"{Path.GetFileName(path)} line {line}: duplicate team '{team.Name}'.");
            }
        }

        return teams;
    }

    private static List<Match> ReadMatches(string path, Dictionary<string, Team> teams, Random random)
    {
        var matches = new List<Match>();

        foreach (var (line, fields) in ReadCsv(path, expectedColumns: 6))
        {
            Team FindTeam(string name) => teams.TryGetValue(name, out var team)
                ? team
                : throw new InvalidDataException($"{Path.GetFileName(path)} line {line}: unknown team '{name}'.");

            var match = new Match
            {
                Round = int.Parse(fields[0], CultureInfo.InvariantCulture),
                // Stored as local wall-clock time (Europe/Sarajevo), exactly as the league publishes it;
                // the app only displays kick-offs, it never compares them across time zones.
                KickOff = DateTime.ParseExact(fields[1], "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture),
                HomeTeam = FindTeam(fields[2]),
                AwayTeam = FindTeam(fields[3])
            };

            var (homeText, awayText) = (fields[4], fields[5]);
            if (homeText.Length > 0 && awayText.Length > 0)
            {
                match.HomeGoals = int.Parse(homeText, CultureInfo.InvariantCulture);
                match.AwayGoals = int.Parse(awayText, CultureInfo.InvariantCulture);
                match.Goals = FictionalPlayers.CreateGoals(random, match.HomeTeam, match.HomeGoals.Value, match.AwayTeam, match.AwayGoals.Value);
            }
            else if (homeText.Length > 0 || awayText.Length > 0)
            {
                throw new InvalidDataException($"{Path.GetFileName(path)} line {line}: a result needs both home and away goals.");
            }

            matches.Add(match);
        }

        return matches;
    }

    private static IEnumerable<(int Line, string[] Fields)> ReadCsv(string path, int expectedColumns)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Seed file not found: {path}", path);
        }

        // ReadAllLines accepts both CRLF and LF endings (teams.csv uses CRLF, the other files LF).
        var lines = File.ReadAllLines(path);

        // Index 0 is the header row; line numbers are 1-based so errors match what an editor shows.
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            // The seed files contain no quoted fields, so a plain split is enough; the column check catches surprises.
            var fields = lines[i].Split(',').Select(f => f.Trim()).ToArray();
            if (fields.Length != expectedColumns)
            {
                throw new InvalidDataException(
                    $"{Path.GetFileName(path)} line {i + 1}: expected {expectedColumns} columns, found {fields.Length}.");
            }

            yield return (i + 1, fields);
        }
    }
}
