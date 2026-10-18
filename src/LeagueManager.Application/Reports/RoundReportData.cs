using LeagueManager.Application.Matches;
using LeagueManager.Application.Standings;
using LeagueManager.Domain.Standings;

namespace LeagueManager.Application.Reports;

public sealed record RoundReportData(
    string LeagueName,
    int Round,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<MatchDetailsDto> Matches,
    IReadOnlyList<StandingRow> Standings,
    IReadOnlyList<TopScorerDto> TopScorers);

public interface IRoundReportRenderer
{
    byte[] Render(RoundReportData report);
}
