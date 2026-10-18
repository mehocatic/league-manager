namespace LeagueManager.Domain.Entities;

public class Goal
{
    // Up to 130 leaves room for stoppage time and extra time; shared by validation and the database check constraint.
    public const int MinMinute = 1;
    public const int MaxMinute = 130;

    public int Id { get; set; }
    public int Minute { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
}