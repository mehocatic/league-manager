namespace LeagueManager.Domain.Entities;

// Stored in the database rather than on disk, so a download endpoint never has to deal with file paths.
public class RoundReport
{
    public int Id { get; set; }
    public int Round { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[] Content { get; set; } = [];
}
