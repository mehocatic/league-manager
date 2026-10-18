namespace LeagueManager.Application.Matches;

// KickOff is local league time (Europe/Sarajevo) with no offset, so the frontend shows it exactly as published.
public sealed record MatchDto(
    int Id,
    int Round,
    DateTime KickOff,
    int HomeTeamId,
    string HomeTeam,
    int AwayTeamId,
    string AwayTeam,
    int? HomeGoals,
    int? AwayGoals)
{
    public bool IsPlayed => HomeGoals is not null && AwayGoals is not null;
}

public sealed record MatchDetailsDto(
    int Id,
    int Round,
    DateTime KickOff,
    int HomeTeamId,
    string HomeTeam,
    int AwayTeamId,
    string AwayTeam,
    int? HomeGoals,
    int? AwayGoals,
    string? Summary,
    IReadOnlyList<GoalDto> Goals)
{
    public bool IsPlayed => HomeGoals is not null && AwayGoals is not null;
}

public sealed record GoalDto(int Minute, int PlayerId, string Player, int TeamId);
