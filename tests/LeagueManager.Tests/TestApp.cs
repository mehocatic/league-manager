using LeagueManager.Application;
using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Infrastructure;
using LeagueManager.Infrastructure.Authentication;
using LeagueManager.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueManager.Tests;

// The same DI setup as the API (minus HTTP) on a throwaway SQLite file, so tests run the real migrations, seeder,
// handlers and cache. Every test gets its own database and its own memory cache, so tests cannot affect each other.
internal sealed class TestApp : IAsyncDisposable
{
    private readonly string _databasePath;
    private readonly ServiceProvider _services;

    private TestApp(string databasePath, ServiceProvider services)
    {
        _databasePath = databasePath;
        _services = services;
    }

    public const string AdminUsername = "test-admin";
    public const string AdminPassword = "test-password";

    public static async Task<TestApp> CreateAsync()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"league-tests-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection()
            .AddLogging()
            .Configure<AdminOptions>(o => (o.Username, o.Password) = (AdminUsername, AdminPassword))
            .Configure<JwtOptions>(o => o.SigningKey = new string('k', JwtOptions.MinSigningKeyBytes))
            .AddApplication()
            .AddInfrastructure($"Data Source={databasePath}", Path.GetTempPath())
            .BuildServiceProvider();

        await services.InitializeDatabaseAsync();
        return new TestApp(databasePath, services);
    }

    // One DI scope per call mirrors one HTTP request in the API.
    public async Task<TResult> QueryAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>().HandleAsync(query, CancellationToken.None);
    }

    public async Task<TResult> CommandAsync<TCommand, TResult>(TCommand command) where TCommand : ICommand<TResult>
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>().HandleAsync(command, CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();

        // Pooled connections keep the file locked on Windows, so they must be closed before the file can be deleted.
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            File.Delete(file);
        }
    }
}

internal static class ResultExtensions
{
    // Tests only care about the happy path here; a failure should fail the test with the handler's message.
    public static T Unwrap<T>(this Result<T> result) =>
        result.Match(value => value, error => throw new InvalidOperationException($"{error.Type}: {error.Message}"));
}
