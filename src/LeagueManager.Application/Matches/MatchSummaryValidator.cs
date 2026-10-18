using System.Globalization;
using System.Text.RegularExpressions;

namespace LeagueManager.Application.Matches;

// LLM output is untrusted: a summary is only stored if it provably states the real result.
internal static partial class MatchSummaryValidator
{
    public const int MinLength = 80;
    public const int MaxLength = 800;

    // Bosnian case endings for nouns, surnames and adjectives ("Viteza", "Jusićem", "Romarima", "Novog").
    // A closed list instead of "any short suffix", so a typo like "Iskrue" is not accepted as "Iskra".
    private static readonly HashSet<string> CaseEndings = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "a", "e", "i", "o", "u", "om", "em", "im", "ih", "og", "oga", "ome", "omu", "ima", "ama",
        // Names ending in -i: "NK Romari" -> "NK Romarija", "NK Romariju", "NK Romarijem".
        "ija", "iju", "ijem"
    };

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

        // When goals are not told in time order, the model calls the last one it mentions "the final goal" even when
        // a later goal exists. Minutes are checked rather than names, because numbers are not declined.
        var words = Words(summary);
        var position = -1;
        foreach (var goal in facts.Goals)
        {
            position = Array.FindIndex(words, position + 1, word => word == goal.Minute.ToString(CultureInfo.InvariantCulture));
            if (position < 0)
            {
                return "goal minutes are missing or not in the order the goals were scored";
            }
        }

        if (summary.Contains("http", StringComparison.OrdinalIgnoreCase) || summary.Contains("**") || summary.Contains('#'))
        {
            return "contains links or markdown";
        }

        return null;
    }

    // Bosnian declines names ("FK VITEZ" -> "FK Viteza", "Jusić" -> "Jusića", "NK NOVI TRAVNIK" -> "NK Novog Travnika"),
    // so every word of the name must appear with the same stem plus one of the case endings.
    // Prefixes (NK/FK) and numbers have no ending, which keeps "NK RUDAR" and "FK RUDAR" apart.
    internal static bool Mentions(string text, string name)
    {
        var textWords = Words(text);
        return Words(name).All(nameWord => textWords.Any(word => IsInflectedForm(word, nameWord)));
    }

    private static bool IsInflectedForm(string word, string nameWord)
    {
        if (nameWord.All(char.IsDigit))
        {
            return word == nameWord;
        }

        // A final vowel belongs to the ending, not the stem: "Sloga" -> "Slogu", "Novi" -> "Novog".
        var stem = nameWord.Length > 3 && "aeiou".Contains(char.ToLowerInvariant(nameWord[^1])) ? nameWord[..^1] : nameWord;

        return word.StartsWith(stem, StringComparison.OrdinalIgnoreCase) && CaseEndings.Contains(word[stem.Length..]);
    }

    private static string[] Words(string text) => WordSeparator().Split(text).Where(w => w.Length > 0).ToArray();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex WordSeparator();
}
