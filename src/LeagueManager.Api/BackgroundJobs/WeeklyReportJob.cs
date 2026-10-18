using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Application.Reports;
using Microsoft.Extensions.Options;

namespace LeagueManager.Api.BackgroundJobs;

// Like a controller, this is only an entry point: the actual work is the same GenerateRoundReport use case
// that the admin can trigger by hand.
public sealed class WeeklyReportJob(
    IServiceScopeFactory scopeFactory, IOptions<ReportOptions> options, TimeProvider timeProvider, ILogger<WeeklyReportJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var report = options.Value;
        if (!report.WeeklyJobEnabled)
        {
            logger.LogInformation("Weekly round report job is disabled");
            return;
        }

        var zone = TimeZoneInfo.FindSystemTimeZoneById(report.TimeZone);

        try
        {
            while (true)
            {
                var nextRun = WeeklySchedule.NextRun(timeProvider.GetUtcNow(), report.DayOfWeek, report.Time, zone);
                logger.LogInformation("Next weekly round report at {NextRun:yyyy-MM-dd HH:mm zzz}", nextRun);

                await Task.Delay(nextRun - timeProvider.GetUtcNow(), timeProvider, stoppingToken);
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            // A hosted service is a singleton, but handlers and the DbContext are scoped, so each run gets its own scope.
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<GenerateRoundReportCommand, Result<RoundReportInfoDto>>>();

            var result = await handler.HandleAsync(new GenerateRoundReportCommand(null), stoppingToken);
            if (!result.TryGetValue(out _, out var error))
            {
                logger.LogWarning("Weekly round report was not generated: {Error}", error.Message);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One failed week must not stop the job for all following weeks.
            logger.LogError(ex, "Weekly round report failed");
        }
    }
}
