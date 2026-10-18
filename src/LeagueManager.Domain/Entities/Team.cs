namespace LeagueManager.Domain.Entities;

public class Team
{
    // Shared by the EF configuration (column size) and Application validation, so the two can never drift apart.
    public const int NameMaxLength = 100;
    public const int CityMaxLength = 100;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    public List<Player> Players { get; set; } = new();
}