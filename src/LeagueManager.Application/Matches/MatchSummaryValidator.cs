namespace LeagueManager.Application.Matches;

// LLM output is untrusted: a summary is only stored if it provably states the real result.
internal static class MatchSummaryValidator
{
    public const int MinLength = 80;
    public const int MaxLength = 800;

    public static string? FindProblem(string summary, MatchFacts facts)
    {
        if (summary.Length is < MinLength or > MaxLength)
        {
            return $"length {summary.Length} is outside {MinLength}-{MaxLength} characters";
        }

        if (!Mentions(summary, facts.HomeTeam) || !Mentions(summary, facts.AwayTeam))
        {
            return "a team name is missing";
        }

        if (!summary.Contains(facts.Score, StringComparison.Ordinal))
        {
            return $"the score {facts.Score} is missing";
        }

        var missingScorers = facts.Goals.Select(g => g.LastName).Distinct().Where(name => !Mentions(summary, name)).ToList();
        if (missingScorers.Count > 0)
        {
            return $"scorer(s) missing: {string.Join(", ", missingScorers)}";
        }

        if (summary.Contains("http", StringComparison.OrdinalIgnoreCase) || summary.Contains("**") || summary.Contains('#'))
        {
            return "contains links or markdown";
        }

        return null;
    }

    private static bool Mentions(string text, string value) => text.Contains(value, StringComparison.OrdinalIgnoreCase);
}
