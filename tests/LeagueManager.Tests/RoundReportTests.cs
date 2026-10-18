using System.Text;
using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Matches;
using LeagueManager.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueManager.Tests;

public class RoundReportTests
{
    private const int LatestPlayedRound = 9;
    private const int MatchesPerRound = 7;

    private static Task<TestApp> CreateAppAsync(FakeLlmClient llm) =>
        TestApp.CreateAsync(services => services.AddSingleton<ILlmClient>(llm));

    [Fact]
    public async Task Report_for_the_latest_round_fills_missing_summaries_and_stores_a_pdf()
    {
        var llm = new FakeLlmClient(FakeLlmClient.ValidSummaryFromPrompt);
        await using var app = await CreateAppAsync(llm);

        var info = (await app.CommandAsync<GenerateRoundReportCommand, Result<RoundReportInfoDto>>(new GenerateRoundReportCommand(null))).Unwrap();

        Assert.Equal(LatestPlayedRound, info.Round);
        Assert.Equal((MatchesPerRound, 0), (info.SummariesGenerated, info.SummariesMissing));

        var round = (await app.QueryAsync<GetMatchesQuery, Result<IReadOnlyList<MatchDto>>>(new GetMatchesQuery(LatestPlayedRound))).Unwrap();
        foreach (var match in round)
        {
            var details = (await app.QueryAsync<GetMatchByIdQuery, Result<MatchDetailsDto>>(new GetMatchByIdQuery(match.Id))).Unwrap();
            Assert.NotNull(details.Summary);
        }

        var pdf = (await app.QueryAsync<GetRoundReportPdfQuery, Result<byte[]>>(new GetRoundReportPdfQuery(LatestPlayedRound))).Unwrap();
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(info.SizeBytes, pdf.Length);
    }

    [Fact]
    public async Task Report_is_still_generated_when_the_ai_fails_and_stops_calling_it()
    {
        var llm = new FakeLlmClient(_ => Error.ExternalService("Gemini is down"));
        await using var app = await CreateAppAsync(llm);

        var info = (await app.CommandAsync<GenerateRoundReportCommand, Result<RoundReportInfoDto>>(new GenerateRoundReportCommand(null))).Unwrap();

        Assert.Equal((0, MatchesPerRound), (info.SummariesGenerated, info.SummariesMissing));
        Assert.Single(llm.Requests);
        Assert.True(info.SizeBytes > 0);
    }

    [Fact]
    public async Task A_rejected_summary_skips_only_that_match()
    {
        const string invalid = "Utakmica je bila zanimljiva i puna borbe, a navijači su uživali u igri obje ekipe do samog kraja.";
        var llm = new FakeLlmClient(_ => invalid, _ => invalid, FakeLlmClient.ValidSummaryFromPrompt);
        await using var app = await CreateAppAsync(llm);

        var info = (await app.CommandAsync<GenerateRoundReportCommand, Result<RoundReportInfoDto>>(new GenerateRoundReportCommand(null))).Unwrap();

        Assert.Equal((MatchesPerRound - 1, 1), (info.SummariesGenerated, info.SummariesMissing));
        Assert.Equal(MatchesPerRound + 1, llm.Requests.Count);
    }

    [Fact]
    public async Task Regenerating_a_round_replaces_its_report()
    {
        var llm = new FakeLlmClient(FakeLlmClient.ValidSummaryFromPrompt);
        await using var app = await CreateAppAsync(llm);

        await app.CommandAsync<GenerateRoundReportCommand, Result<RoundReportInfoDto>>(new GenerateRoundReportCommand(3));
        await app.CommandAsync<GenerateRoundReportCommand, Result<RoundReportInfoDto>>(new GenerateRoundReportCommand(3));

        var reports = await app.QueryAsync<GetRoundReportsQuery, IReadOnlyList<RoundReportListItemDto>>(new GetRoundReportsQuery());
        Assert.Equal(3, Assert.Single(reports).Round);

        // Summaries already stored by the first run are not generated (and paid for) again.
        Assert.Equal(MatchesPerRound, llm.Requests.Count);
    }

    [Fact]
    public async Task Unknown_round_is_not_found()
    {
        await using var app = await CreateAppAsync(new FakeLlmClient());

        var result = await app.CommandAsync<GenerateRoundReportCommand, Result<RoundReportInfoDto>>(new GenerateRoundReportCommand(99));

        Assert.Equal(ErrorType.NotFound, result.Match(_ => (ErrorType?)null, e => e.Type));
    }
}
