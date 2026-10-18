namespace LeagueManager.Infrastructure.Authentication;

// Only used to create the first admin when the Users table is empty; changing it later does not change the stored password.
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string Username { get; set; } = string.Empty;

    // Never in appsettings: comes from dotnet user-secrets or the Admin__Password environment variable.
    public string Password { get; set; } = string.Empty;

    // Set by the host (true only in Development), so a fresh clone works with just `dotnet run`.
    public bool GeneratePasswordIfMissing { get; set; }
}
