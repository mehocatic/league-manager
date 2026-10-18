using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeagueManager.Application.Matches;

public sealed record GenerateMatchSummaryCommand(int MatchId) : ICommand<Result<MatchDetailsDto>>;

internal sealed class GenerateMatchSummaryCommandHandler(
    ILeagueDbContext db, ILlmClient llm, ILogger<GenerateMatchSummaryCommandHandler> logger)
    : ICommandHandler<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>
{
    // One retry is usually enough when the model drops a fact; more would only multiply the cost.
    private const int MaxAttempts = 2;

    public async Task<Result<MatchDetailsDto>> HandleAsync(GenerateMatchSummaryCommand command, CancellationToken cancellationToken)
    {
        var match = await db.Matches
            .Where(m => m.Id == command.MatchId)
            .Select(m => new
            {
                m.Round,
                m.HomeTeamId,
                HomeTeam = m.HomeTeam.Name,
                AwayTeam = m.AwayTeam.Name,
                m.HomeGoals,
                m.AwayGoals,
                // Only the scorer's TeamId comes from SQL: resolving the team name inside this sub-query
                // would need an APPLY join, which SQLite does not support.
                Goals = m.Goals
                    .OrderBy(g => g.Minute)
                    .Select(g => new { g.Minute, g.Player.FirstName, g.Player.LastName, g.Player.TeamId })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return Error.NotFound($"Match {command.MatchId} was not found.");
        }

        if (match.HomeGoals is not { } homeGoals || match.AwayGoals is not { } awayGoals)
        {
            return Error.Conflict($"Match {command.MatchId} has no result yet, so there is nothing to summarise.");
        }

        // The running score is computed here, because the model got the order of goals wrong when it had to infer it
        // (it called a 70th minute goal "final" although another goal followed in the 88th).
        var (home, away) = (0, 0);
        var goals = new List<GoalFact>(match.Goals.Count);
        foreach (var g in match.Goals)
        {
            var isHomeGoal = g.TeamId == match.HomeTeamId;
            if (isHomeGoal) home++; else away++;

            goals.Add(new GoalFact(
                g.Minute, $"{g.FirstName} {g.LastName}", g.LastName, isHomeGoal ? match.HomeTeam : match.AwayTeam, $"{home}:{away}"));
        }
        var facts = new MatchFacts(match.Round, match.HomeTeam, match.AwayTeam, homeGoals, awayGoals, goals);
        var request = MatchSummaryPrompt.Create(command.MatchId, facts);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (!(await llm.GenerateTextAsync(request, cancellationToken)).TryGetValue(out var text, out var error))
            {
                return error;
            }

            var summary = text.Trim();
            var problem = MatchSummaryValidator.FindProblem(summary, facts);
            if (problem is null)
            {
                await db.Matches
                    .Where(m => m.Id == command.MatchId)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.Summary, summary), cancellationToken);

                return await db.Matches.Where(m => m.Id == command.MatchId).SelectDetails().SingleAsync(cancellationToken);
            }

            // The rejected text is logged because it is what shows how to improve the prompt.
            logger.LogWarning(
                "Rejected AI summary for match {MatchId} (attempt {Attempt}): {Problem}. Text: {Summary}", command.MatchId, attempt, problem, summary);
        }

        return RejectedError;
    }

    // A known instance, so callers can tell "this match's text was rejected" from "the AI service is unavailable".
    public static readonly Error RejectedError = Error.ExternalService("The AI summary did not pass validation. Please try again.");
}
