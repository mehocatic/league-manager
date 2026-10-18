namespace LeagueManager.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int ShirtNumber { get; set; }

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!; 
}