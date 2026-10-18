namespace LeagueManager.Domain.Entities;

public class User
{
    public const int UsernameMaxLength = 50;

    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Admin;
}

public static class Roles
{
    public const string Admin = "Admin";
}
