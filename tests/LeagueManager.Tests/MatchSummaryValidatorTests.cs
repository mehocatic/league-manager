using LeagueManager.Application.Matches;

namespace LeagueManager.Tests;

public class MatchSummaryValidatorTests
{
    private static readonly MatchFacts Facts = new(10, "NK SLOGA", "FK VITEZ", 2, 1,
    [
        new GoalFact(12, "Edin Hodžić", "Hodžić", "NK SLOGA", "1:0"),
        new GoalFact(55, "Marko Jurić", "Jurić", "FK VITEZ", "1:1"),
        new GoalFact(81, "Amar Begić", "Begić", "NK SLOGA", "2:1")
    ]);

    // Names are declined as in real Bosnian text: "FK Viteza", "Hodžića", "Begićevim".
    private const string Valid =
        "NK Sloga je u 10. kolu savladala FK Viteza rezultatom 2:1. Vodstvo je donio pogodak Edina Hodžića u 12. minuti, " +
        "Marko Jurić je izjednačio u 55. minuti, a pobjedu domaćima osigurao je Amar Begić u 81. minuti.";

    [Fact]
    public void Accepts_a_summary_with_declined_team_and_scorer_names() =>
        Assert.Null(MatchSummaryValidator.FindProblem(Valid, Facts));

    [Theory]
    [InlineData("2:1", "1:2", "the score 2:1 is missing")]
    [InlineData("Amar Begić", "Amar Smith", "scorer(s) missing: Begić")]
    [InlineData("FK Viteza", "Viteza", "a team name is missing")]
    [InlineData("10. kolu", "10. kolu (**detalji** na https://example.com)", "contains links or markdown")]
    public void Rejects_a_summary_that_misses_or_invents_facts(string original, string replacement, string expectedProblem) =>
        Assert.Equal(expectedProblem, MatchSummaryValidator.FindProblem(Valid.Replace(original, replacement), Facts));

    [Theory]
    [InlineData( // grouped by team: the 55th minute goal is told last and wrongly called final
        "NK Sloga je u 10. kolu savladala FK Viteza rezultatom 2:1. Za domaće su pogađali Edin Hodžić u 12. i Amar Begić u 81. minuti, " +
        "a konačan rezultat postavio je Marko Jurić u 55. minuti.")]
    [InlineData( // minutes of the later goals left out
        "NK Sloga je u 10. kolu savladala FK Viteza rezultatom 2:1. Edin Hodžić je pogodio u 12. minuti, " +
        "a do kraja su se upisali još Marko Jurić i Amar Begić.")]
    public void Rejects_goals_not_told_in_time_order_with_their_minutes(string summary) =>
        Assert.Equal("goal minutes are missing or not in the order the goals were scored", MatchSummaryValidator.FindProblem(summary, Facts));

    [Fact]
    public void Rejects_real_gemini_output_that_called_the_70th_minute_goal_final()
    {
        var facts = new MatchFacts(1, "NK ROMARI", "NK ISKRA 1946", 2, 5,
        [
            new GoalFact(31, "Vedad Imamović", "Imamović", "NK ROMARI", "1:0"),
            new GoalFact(49, "Nedim Softić", "Softić", "NK ISKRA 1946", "1:1"),
            new GoalFact(55, "Almir Marić", "Marić", "NK ISKRA 1946", "1:2"),
            new GoalFact(58, "Benjamin Čolić", "Čolić", "NK ISKRA 1946", "1:3"),
            new GoalFact(68, "Almir Marić", "Marić", "NK ISKRA 1946", "1:4"),
            new GoalFact(70, "Elvir Hasanović", "Hasanović", "NK ROMARI", "2:4"),
            new GoalFact(88, "Almir Marić", "Marić", "NK ISKRA 1946", "2:5")
        ]);
        const string geminiOutput =
            "U prvom kolu NK Romari je na svom terenu dočekao NK Iskra 1946, a utakmica je završena rezultatom 2:5. " +
            "Domaći su poveli u 31. minuti preko Vedada Imamovića. U nastavku susreta NK Iskra 1946 je preokrenula rezultat serijom pogodaka, " +
            "koje su postigli Nedim Softić u 49., Almir Marić u 55., 68. i 88. minuti te Benjamin Čolić u 58. minuti. " +
            "Konačan rezultat postavio je Elvir Hasanović u 70. minuti za NK Romari.";

        Assert.Equal("goal minutes are missing or not in the order the goals were scored", MatchSummaryValidator.FindProblem(geminiOutput, facts));
    }

    [Fact]
    public void Rejects_a_summary_that_is_too_short_or_too_long()
    {
        Assert.StartsWith("length", MatchSummaryValidator.FindProblem("NK Sloga 2:1 FK Vitez", Facts));
        Assert.StartsWith("length", MatchSummaryValidator.FindProblem(Valid + new string('x', MatchSummaryValidator.MaxLength), Facts));
    }

    [Theory]
    [InlineData("FK VITEZ", "pobjeda FK Viteza")]
    [InlineData("FK VITEZ", "protiv FK Vitezom")]
    [InlineData("NK SLOGA", "NK Slogu je")]
    [InlineData("NK NOVI TRAVNIK", "gosti iz NK Novog Travnika")]
    [InlineData("NK ISKRA 1946", "golovi NK Iskre 1946")]
    [InlineData("NK ROMARI", "protiv NK Romarima")]
    [InlineData("NK ROMARI", "pobjeda NK Romarija")]
    [InlineData("GNK BRATSTVO", "remi GNK Bratstva")]
    [InlineData("Jusić", "pogodak Jusića")]
    [InlineData("Ibrahimović", "Ibrahimovićem")]
    public void Accepts_bosnian_case_forms(string name, string text) =>
        Assert.True(MatchSummaryValidator.Mentions(text, name));

    [Theory]
    [InlineData("FK VITEZ", "pobjeda Viteza")]
    [InlineData("NK RUDAR", "pobjeda FK Rudara")]
    [InlineData("NK ISKRA 1946", "golovi NK Iskre")]
    [InlineData("Jurić", "pogodak Jurišića")]
    [InlineData("NK SLOGA", "NK Slobode")]
    [InlineData("NK ISKRA 1946", "za NK Iskrue 1946")]
    [InlineData("NK BAJER 99", "savladao NK Bajr 99")]
    [InlineData("Jusić", "Jusićevi")]
    public void Rejects_missing_prefix_number_or_a_different_name(string name, string text) =>
        Assert.False(MatchSummaryValidator.Mentions(text, name));
}
