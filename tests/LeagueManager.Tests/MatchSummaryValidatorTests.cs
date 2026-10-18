using LeagueManager.Application.Matches;

namespace LeagueManager.Tests;

public class MatchSummaryValidatorTests
{
    private static readonly MatchFacts Facts = new(10, "NK SLOGA", "FK VITEZ", 2, 1,
    [
        new GoalFact(12, "Edin Hodžić", "Hodžić", "NK SLOGA"),
        new GoalFact(55, "Marko Jurić", "Jurić", "FK VITEZ"),
        new GoalFact(81, "Amar Begić", "Begić", "NK SLOGA")
    ]);

    private const string Valid =
        "NK SLOGA beat FK VITEZ 2:1 in round 10. Edin Hodžić opened the scoring in the 12th minute, " +
        "Marko Jurić equalised in the 55th, and Amar Begić won it for the hosts in the 81st minute.";

    [Fact]
    public void Accepts_a_summary_with_teams_score_and_all_scorers() =>
        Assert.Null(MatchSummaryValidator.FindProblem(Valid, Facts));

    [Theory]
    [InlineData("2:1", "1:2", "the score 2:1 is missing")]
    [InlineData("Amar Begić", "Amar Smith", "scorer(s) missing: Begić")]
    [InlineData("FK VITEZ", "Vitez", "a team name is missing")]
    [InlineData("round 10.", "round 10. See **details** at https://example.com.", "contains links or markdown")]
    public void Rejects_a_summary_that_misses_or_invents_facts(string original, string replacement, string expectedProblem) =>
        Assert.Equal(expectedProblem, MatchSummaryValidator.FindProblem(Valid.Replace(original, replacement), Facts));

    [Fact]
    public void Rejects_a_summary_that_is_too_short_or_too_long()
    {
        Assert.StartsWith("length", MatchSummaryValidator.FindProblem("NK SLOGA 2:1 FK VITEZ", Facts));
        Assert.StartsWith("length", MatchSummaryValidator.FindProblem(Valid + new string('x', MatchSummaryValidator.MaxLength), Facts));
    }
}
