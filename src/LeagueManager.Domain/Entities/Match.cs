namespace LeagueManager.Domain.Entities;

public class Match
{
    public int Id { get; set; }
    public int Round { get; set; }
    public DateTime KickOff { get; set; }

    public int HomeTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;
    public int AwayTeamId { get; set; }
    public Team AwayTeam { get; set; } = null!;

    public int? HomeGoals { get; set; }
    public int? AwayGoals { get; set; }

    public string? Summary { get; set; }

    public List<Goal> Goals { get; set; } = new();

    public bool IsPlayed => HomeGoals.HasValue && AwayGoals.HasValue;
}