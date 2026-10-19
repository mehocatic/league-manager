using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Matches;
using LeagueManager.Application.Standings;
using LeagueManager.Domain.Entities;
using LeagueManager.Domain.Standings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeagueManager.Application.Reports;

public sealed record RoundReportInfoDto(int Round, DateTime CreatedAtUtc, int SizeBytes, int SummariesGenerated, int SummariesMissing);

// Round null means "the latest round with a result", which is what the Monday job reports on.
public sealed record GenerateRoundReportCommand(int? Round) : ICommand<Result<RoundReportInfoDto>>;

internal sealed class GenerateRoundReportCommandHandler(
    ILeagueDbContext db,
    ICommandHandler<GenerateMatchSummaryCommand, Result<MatchDetailsDto>> generateSummary,
    IQueryHandler<GetStandingsQuery, IReadOnlyList<StandingRow>> getStandings,
    IQueryHandler<GetTopScorersQuery, Result<IReadOnlyList<TopScorerDto>>> getTopScorers,
    IRoundReportRenderer renderer,
    IOptions<ReportOptions> options,
    TimeProvider timeProvider,
    ILogger<GenerateRoundReportCommandHandler> logger)
    : ICommandHandler<GenerateRoundReportCommand, Result<RoundReportInfoDto>>
{
    private const int TopScorersInReport = 10;

    public async Task<Result<RoundReportInfoDto>> HandleAsync(GenerateRoundReportCommand command, CancellationToken cancellationToken)
    {
        var round = command.Round ?? await db.Matches
            .Where(m => m.HomeGoals != null && m.AwayGoals != null)
            .MaxAsync(m => (int?)m.Round, cancellationToken);

        if (round is null)
        {
            return Error.Conflict("No match has been played yet, so there is no round to report on.");
        }

        if (!await db.Matches.AnyAsync(m => m.Round == round, cancellationToken))
        {
            return Error.NotFound($"Round {round} has no matches.");
        }

        var summariesGenerated = await FillMissingSummariesAsync(round.Value, cancellationToken);

        var matches = await db.Matches
            .Where(m => m.Round == round)
            .OrderBy(m => m.KickOff).ThenBy(m => m.Id)
            .SelectDetails()
            .ToListAsync(cancellationToken);
        var standings = await getStandings.HandleAsync(new GetStandingsQuery(round), cancellationToken);
        var topScorers = (await getTopScorers.HandleAsync(new GetTopScorersQuery(TopScorersInReport, round), cancellationToken))
            .Match(scorers => scorers, _ => []);

        var report = options.Value;
        var now = timeProvider.GetUtcNow();
        var generatedAt = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(report.TimeZone));
        var pdf = renderer.Render(new RoundReportData(report.LeagueName, round.Value, generatedAt, matches, standings, topScorers));

        var stored = await db.RoundReports.SingleOrDefaultAsync(r => r.Round == round, cancellationToken);
        if (stored is null)
        {
            stored = new RoundReport { Round = round.Value };
            db.RoundReports.Add(stored);
        }

        stored.CreatedAtUtc = now.UtcDateTime;
        stored.Content = pdf;
        await db.SaveChangesAsync(cancellationToken);

        var summariesMissing = matches.Count(m => m.IsPlayed && m.Summary is null);
        logger.LogInformation(
            "Round {Round} report generated: {SizeBytes} bytes, {Generated} summaries generated, {Missing} still missing",
            round, pdf.Length, summariesGenerated, summariesMissing);

        return new RoundReportInfoDto(round.Value, stored.CreatedAtUtc, pdf.Length, summariesGenerated, summariesMissing);
    }

    private async Task<int> FillMissingSummariesAsync(int round, CancellationToken cancellationToken)
    {
        var matchIds = await db.Matches
            .Where(m => m.Round == round && m.HomeGoals != null && m.AwayGoals != null && m.Summary == null)
            .OrderBy(m => m.Id)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var generated = 0;
        foreach (var matchId in matchIds)
        {
            if ((await generateSummary.HandleAsync(new GenerateMatchSummaryCommand(matchId), cancellationToken)).TryGetValue(out _, out var error))
            {
                generated++;
                continue;
            }

            if (error == GenerateMatchSummaryCommandHandler.RejectedError)
            {
                // Rejected text is specific to this match, so the other matches are still worth a try.
                continue;
            }

            // The PDF must not depend on the AI: if Gemini is down or not configured, the remaining calls would fail
            // the same way, so the report is produced without the missing summaries.
            logger.LogWarning("AI summaries skipped for round {Round} after match {MatchId} failed: {Error}", round, matchId, error.Message);
            break;
        }

        return generated;
    }
}
