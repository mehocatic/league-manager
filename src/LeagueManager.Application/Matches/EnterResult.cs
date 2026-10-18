using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Matches;

public sealed record GoalInput(int PlayerId, int Minute);

// Replaces the whole result, so the same command both enters a new result and corrects a wrong one.
public sealed record EnterResultCommand(int MatchId, int HomeGoals, int AwayGoals, IReadOnlyList<GoalInput> Goals)
    : ICommand<Result<MatchDetailsDto>>;

internal sealed class EnterResultCommandHandler(ILeagueDbContext db) : ICommandHandler<EnterResultCommand, Result<MatchDetailsDto>>
{
    public async Task<Result<MatchDetailsDto>> HandleAsync(EnterResultCommand command, CancellationToken cancellationToken)
    {
        // Checks that need no database run first, so obviously bad input never costs a query.
        if (command.HomeGoals < 0 || command.AwayGoals < 0)
        {
            return Error.Validation("Goals cannot be negative.");
        }

        var totalGoals = command.HomeGoals + command.AwayGoals;
        if (command.Goals.Count != totalGoals)
        {
            return Error.Validation(
                $"A {command.HomeGoals}:{command.AwayGoals} result needs exactly {totalGoals} scorers, but {command.Goals.Count} were given.");
        }

        if (command.Goals.FirstOrDefault(g => g.Minute is < Goal.MinMinute or > Goal.MaxMinute) is { } badMinute)
        {
            return Error.Validation($"Minute {badMinute.Minute} is invalid; it must be between {Goal.MinMinute} and {Goal.MaxMinute}.");
        }

        var match = await db.Matches
            .Include(m => m.Goals)
            .SingleOrDefaultAsync(m => m.Id == command.MatchId, cancellationToken);

        if (match is null)
        {
            return Error.NotFound($"Match {command.MatchId} was not found.");
        }

        // One query for all scorers instead of one per goal; we only need to know which team each player is in.
        var playerIds = command.Goals.Select(g => g.PlayerId).Distinct().ToList();
        var teamByPlayer = await db.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.TeamId, cancellationToken);

        var unknownIds = playerIds.Where(id => !teamByPlayer.ContainsKey(id)).ToList();
        if (unknownIds.Count > 0)
        {
            return Error.Validation($"Unknown player id(s): {string.Join(", ", unknownIds)}.");
        }

        var outsiders = playerIds
            .Where(id => teamByPlayer[id] != match.HomeTeamId && teamByPlayer[id] != match.AwayTeamId)
            .ToList();
        if (outsiders.Count > 0)
        {
            return Error.Validation($"Player id(s) {string.Join(", ", outsiders)} do not play for either team in this match.");
        }

        // The total already matches, so a correct home count also guarantees a correct away count.
        var homeScorers = command.Goals.Count(g => teamByPlayer[g.PlayerId] == match.HomeTeamId);
        if (homeScorers != command.HomeGoals)
        {
            return Error.Validation(
                $"The home team scored {command.HomeGoals} goal(s), but {homeScorers} home scorer(s) were given.");
        }

        match.HomeGoals = command.HomeGoals;
        match.AwayGoals = command.AwayGoals;

        // Removed goals are orphans of a required relationship, so EF deletes them; corrections leave no stale scorers.
        match.Goals.Clear();
        match.Goals.AddRange(command.Goals.Select(g => new Goal { PlayerId = g.PlayerId, Minute = g.Minute }));

        // A summary written for the old result would now be wrong; it is regenerated in the AI summary milestone.
        match.Summary = null;

        await db.SaveChangesAsync(cancellationToken);

        return await db.Matches
            .Where(m => m.Id == match.Id)
            .SelectDetails()
            .SingleAsync(cancellationToken);
    }
}
