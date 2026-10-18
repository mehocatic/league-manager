using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueManager.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // The DbContext is scoped (one per HTTP request); at startup there is no request, so we open a scope ourselves.
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LeagueDbContext>();

        // Applying pending migrations here means a fresh clone needs only `dotnet run`, no separate `dotnet ef database update`.
        await db.Database.MigrateAsync(cancellationToken);
    }
}
