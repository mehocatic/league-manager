using LeagueManager.Application.Abstractions;
using LeagueManager.Infrastructure.Ai;
using LeagueManager.Infrastructure.Authentication;
using LeagueManager.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LeagueManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString, string contentRootPath)
    {
        // SQLite resolves a relative path against the process working directory, so starting the app
        // from another folder would silently create a second, empty database. Anchoring it to the
        // content root makes `dotnet run`, `dotnet ef` and every machine use the same file.
        var sqlite = new SqliteConnectionStringBuilder(connectionString);
        if (!Path.IsPathRooted(sqlite.DataSource))
        {
            sqlite.DataSource = Path.Combine(contentRootPath, sqlite.DataSource);
        }

        services.AddDbContext<LeagueDbContext>(options => options.UseSqlite(sqlite.ToString()));

        // Resolves to the same scoped instance, so a handler and anything else in the request share one unit of work.
        services.AddScoped<ILeagueDbContext>(provider => provider.GetRequiredService<LeagueDbContext>());

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtTokenIssuer>();

        services.AddHttpClient<ILlmClient, GeminiClient>((provider, http) =>
        {
            var gemini = provider.GetRequiredService<IOptions<GeminiOptions>>().Value;
            http.BaseAddress = new Uri(gemini.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(gemini.TimeoutSeconds);
        });

        return services;
    }
}
