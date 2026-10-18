namespace LeagueManager.Domain.Entities;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public List<Player> Players { get; set; } = new();
}