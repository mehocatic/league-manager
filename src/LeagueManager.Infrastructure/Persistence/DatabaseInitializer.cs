using LeagueManager.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeagueManager.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // The DbContext is scoped (one per HTTP request); at startup there is no request, so we open a scope ourselves.
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        // Applying pending migrations here means a fresh clone needs only `dotnet run`, no separate `dotnet ef database update`.
        await db.Database.MigrateAsync(cancellationToken);

        // The CSVs are linked into the build output (see LeagueManager.Infrastructure.csproj), so this works from any working directory.
        await LeagueSeeder.SeedAsync(db, Path.Combine(AppContext.BaseDirectory, "SeedData"), logger, cancellationToken);
    }
}
