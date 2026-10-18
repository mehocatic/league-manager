using System.Text.Json;
using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;

namespace LeagueManager.Tests;

// Replays scripted answers in order; the last answer repeats, so one answer can serve a whole round.
internal sealed class FakeLlmClient(params Func<LlmRequest, Result<string>>[] answers) : ILlmClient
{
    public List<LlmRequest> Requests { get; } = [];

    public Task<Result<string>> GenerateTextAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(answers[Math.Min(Requests.Count, answers.Length) - 1](request));
    }

    // Builds a summary that passes validation from the facts the handler put into the prompt.
    public static Result<string> ValidSummaryFromPrompt(LlmRequest request)
    {
        var facts = JsonDocument.Parse(request.Prompt[(request.Prompt.IndexOf('\n') + 1)..]).RootElement;
        var goals = facts.GetProperty("goals").EnumerateArray()
            .Select(g => $"{g.GetProperty("scorer").GetString()} je pogodio u {g.GetProperty("minute").GetInt32()}. minuti.");

        return $"{facts.GetProperty("homeTeam").GetString()} i {facts.GetProperty("awayTeam").GetString()} odigrali su utakmicu " +
               $"{facts.GetProperty("round").GetInt32()}. kola, završenu rezultatom {facts.GetProperty("finalScore").GetString()}. " +
               string.Join(" ", goals);
    }
}
