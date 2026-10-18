namespace LeagueManager.Domain.Entities;

public class Goal
{
    public int Id { get; set; }
    public int Minute { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
}