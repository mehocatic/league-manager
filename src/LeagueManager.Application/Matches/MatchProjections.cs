using LeagueManager.Domain.Entities;

namespace LeagueManager.Application.Matches;

// Shared projections, so the list, the details endpoint and the enter-result response always map a match the same way.
// They stay IQueryable, so EF translates them into one SQL query with only the needed columns and joins.
internal static class MatchProjections
{
    public static IQueryable<MatchDto> SelectDto(this IQueryable<Match> matches) =>
        matches.Select(m => new MatchDto(
            m.Id, m.Round, m.KickOff,
            m.HomeTeamId, m.HomeTeam.Name,
            m.AwayTeamId, m.AwayTeam.Name,
            m.HomeGoals, m.AwayGoals));

    public static IQueryable<MatchDetailsDto> SelectDetails(this IQueryable<Match> matches) =>
        matches.Select(m => new MatchDetailsDto(
            m.Id, m.Round, m.KickOff,
            m.HomeTeamId, m.HomeTeam.Name,
            m.AwayTeamId, m.AwayTeam.Name,
            m.HomeGoals, m.AwayGoals,
            m.Summary,
            m.Goals
                .OrderBy(g => g.Minute)
                .Select(g => new GoalDto(g.Minute, g.PlayerId, g.Player.FirstName + " " + g.Player.LastName, g.Player.TeamId))
                .ToList()));
}
