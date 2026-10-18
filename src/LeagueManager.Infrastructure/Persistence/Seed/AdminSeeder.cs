using System.Security.Cryptography;
using LeagueManager.Application.Abstractions;
using LeagueManager.Domain.Entities;
using LeagueManager.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeagueManager.Infrastructure.Persistence.Seed;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        LeagueDbContext db, IPasswordHasher passwordHasher, AdminOptions admin, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var password = admin.Password;
        var generated = false;

        if (string.IsNullOrWhiteSpace(password) && admin.GeneratePasswordIfMissing)
        {
            password = RandomNumberGenerator.GetString(PasswordAlphabet, GeneratedPasswordLength);
            generated = true;
        }

        if (string.IsNullOrWhiteSpace(admin.Username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No admin user exists and Admin:Username / Admin:Password are not configured, so nobody can log in");
            return;
        }

        db.Users.Add(new User { Username = admin.Username, PasswordHash = passwordHasher.Hash(password), Role = Roles.Admin });
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created admin user '{Username}'", admin.Username);

        // Logging a password is acceptable only here: it is generated in Development, only for a local database,
        // and printed once because only its hash is stored. Delete league.db to get a new one.
        if (generated)
        {
            logger.LogWarning("Admin password: {Password}", password);
        }
    }

    // No look-alike characters (0/O, 1/l/I), so the password is easy to copy from the console by hand.
    private const string PasswordAlphabet = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int GeneratedPasswordLength = 16;
}
