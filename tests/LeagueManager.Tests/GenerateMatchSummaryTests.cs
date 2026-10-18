using System.Text.Json;
using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Matches;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueManager.Tests;

public class GenerateMatchSummaryTests
{
    // Replays scripted answers; a function per answer lets it build text from the real seeded facts in the prompt.
    private sealed class FakeLlmClient(params Func<LlmRequest, Result<string>>[] answers) : ILlmClient
    {
        public List<LlmRequest> Requests { get; } = [];

        public Task<Result<string>> GenerateTextAsync(LlmRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(answers[Requests.Count - 1](request));
        }
    }

    private static async Task<(TestApp App, MatchDetailsDto Match)> SetUpAsync(FakeLlmClient llm)
    {
        var app = await TestApp.CreateAsync(services => services.AddSingleton<ILlmClient>(llm));
        var match = (await app.QueryAsync<GetMatchByIdQuery, Result<MatchDetailsDto>>(new GetMatchByIdQuery(1))).Unwrap();
        return (app, match);
    }

    private static string ValidSummary(MatchDetailsDto m) =>
        $"{m.HomeTeam} and {m.AwayTeam} played in round {m.Round} and the match finished {m.HomeGoals}:{m.AwayGoals}. Goals: " +
        string.Join(", ", m.Goals.Select(g => $"{g.Player} ({g.Minute}')")) + ".";

    [Fact]
    public async Task Valid_summary_is_stored_on_the_match()
    {
        MatchDetailsDto? match = null;
        var llm = new FakeLlmClient(_ => ValidSummary(match!));
        (var app, match) = await SetUpAsync(llm);
        await using var _ = app;

        var result = (await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(match.Id))).Unwrap();

        Assert.Equal(ValidSummary(match), result.Summary);
        Assert.Single(llm.Requests);
        Assert.Contains(match.HomeTeam, llm.Requests[0].Prompt);
    }

    [Fact]
    public async Task Prompt_contains_the_running_score_after_each_goal()
    {
        MatchDetailsDto? match = null;
        var llm = new FakeLlmClient(_ => ValidSummary(match!));
        (var app, match) = await SetUpAsync(llm);
        await using var _ = app;

        await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(match.Id));

        var (home, away) = (0, 0);
        var expected = match.Goals.Select(g => g.TeamId == match.HomeTeamId ? $"{++home}:{away}" : $"{home}:{++away}").ToList();

        var json = llm.Requests[0].Prompt[(llm.Requests[0].Prompt.IndexOf('\n') + 1)..];
        var sent = JsonDocument.Parse(json).RootElement.GetProperty("goals").EnumerateArray()
            .Select(g => g.GetProperty("scoreAfter").GetString()!)
            .ToList();

        Assert.Equal(expected, sent);
        Assert.Equal($"{match.HomeGoals}:{match.AwayGoals}", sent[^1]);
    }

    [Fact]
    public async Task Invalid_first_answer_is_retried_once()
    {
        MatchDetailsDto? match = null;
        var llm = new FakeLlmClient(_ => "A great game of football was played today between two very determined teams.", _ => ValidSummary(match!));
        (var app, match) = await SetUpAsync(llm);
        await using var _ = app;

        var result = (await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(match.Id))).Unwrap();

        Assert.Equal(2, llm.Requests.Count);
        Assert.Equal(ValidSummary(match), result.Summary);
    }

    [Fact]
    public async Task Two_invalid_answers_return_an_error_and_store_nothing()
    {
        const string invented = "Both teams were brilliant, and the 5:5 thriller will be remembered for years by everyone who was there.";
        var llm = new FakeLlmClient(_ => invented, _ => invented);
        var (app, match) = await SetUpAsync(llm);
        await using var _ = app;

        var result = await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(match.Id));

        Assert.Equal(ErrorType.ExternalService, result.Match(_ => (ErrorType?)null, e => e.Type));
        var stored = (await app.QueryAsync<GetMatchByIdQuery, Result<MatchDetailsDto>>(new GetMatchByIdQuery(match.Id))).Unwrap();
        Assert.Null(stored.Summary);
    }

    [Fact]
    public async Task Unplayed_match_is_a_conflict_and_the_llm_is_not_called()
    {
        var llm = new FakeLlmClient();
        var (app, _) = await SetUpAsync(llm);
        await using var __ = app;
        var scheduled = (await app.QueryAsync<GetMatchesQuery, Result<IReadOnlyList<MatchDto>>>(new GetMatchesQuery(13))).Unwrap()[0];

        var result = await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(scheduled.Id));

        Assert.Equal(ErrorType.Conflict, result.Match(_ => (ErrorType?)null, e => e.Type));
        Assert.Empty(llm.Requests);
    }
}
